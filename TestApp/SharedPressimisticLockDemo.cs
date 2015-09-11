using System;

namespace TestApp
{
    using Infrastructure.Lock;
    using Infrastructure.Mapping;
    using Infrastructure.Session;
    using Infrastructure.UnitOfWork;
    using Model.Customers;
    using Repository;
    using Repository.Mapping.SQL;
    using Repository.UnitOfWork;
    using Session;

    public sealed class TestInfo
    {
        public void Initialize(Guid entityId, string name)
        {
            this.EntityId = entityId;
            this.SessionId = SessionManager.Manager.Open(name);
            this.uow = new UnitOfWork();
            this.customerMapper = new CustomerSQLMapper();
            this.repository = new CustomerRepository(this.uow, customerMapper);
        }

        public Guid SessionId;
        public Guid EntityId;
        public Customer Entity;
        public IUnitOfWork uow;
        public ICustomerRepository repository;
        public IDataMapper<Customer> customerMapper;

        public ISession GetSession()
        {
            return SessionManager.Manager.GetSession(SessionId);
        }

        public void LoadCustomer()
        {
            SetCurrentSession();
            var connection = GetSession().DbInfo.Connection;
            connection.Open();
            try
            {
                //if (GetSession().LockManager.GetLock(EntityId, LockType.Read))
                    this.Entity = this.repository.FindBy(EntityId);
            }
            finally
            {
                connection.Close();
            }
        }

        public void EditCustomer(string newName)
        {
            SetCurrentSession();
            var connection = GetSession().DbInfo.Connection;
            connection.Open();
            try
            {
                //if (GetSession().LockManager.GetLock(EntityId, LockType.Write))
                    Entity.FirstName = newName;
            }
            finally
            {
                connection.Close();
            }
        }

        public void SaveCustomer()
        {
            SetCurrentSession();
            var connection = GetSession().DbInfo.Connection;
            connection.Open();
            try
            {
                if (GetSession().LockManager.GetLock(Entity.VersionId, LockType.Write)) //This should be only a EnsureLock(LockType.Write) - TODO
                {
                    this.repository.Update(Entity);
                    this.uow.Commit();
                }
            }
            catch (Exception e)
            {
                System.Console.WriteLine(e.Message);
            }
            finally
            {
                GetSession().LockManager.GetLock(Entity.VersionId, LockType.Read);
                connection.Close();
            }
        }

        public void ReleaseCustomer()
        {
            SetCurrentSession();
            var connection = GetSession().DbInfo.Connection;
            connection.Open();
            try
            {
                GetSession().LockManager.ReleaseLock(Entity.VersionId);
                EntityId = Guid.Empty;
                Entity = null;
            }
            finally
            {
                connection.Close();
            }
        }

        private void SetCurrentSession()
        {
            SessionManager.Manager.Current = SessionId;
        }
    }

    public class SharedPressimisticLockDemo
    {
        public SharedPressimisticLockDemo() { }

        public void EditSameEntity()
        {
            //TEST 1 - No one gets write lock! - no modifications
            Console.WriteLine("SCENARIO 1");
            var id = new Guid("964d22a1-3387-43e0-b347-ea5e2fed1659");
            var manager = SessionManager.Manager;

            var user1 = new TestInfo();
            user1.Initialize(id, "User1");

            var user2 = new TestInfo();
            user2.Initialize(id, "User2");

            var user3 = new TestInfo();
            user3.Initialize(id, "User3");

            user1.LoadCustomer();
            user2.LoadCustomer();

            user1.EditCustomer("KaikeU1");
            user2.EditCustomer("KaikeU2");

            user2.SaveCustomer();
            user3.LoadCustomer(); // <-- User 3 reads after user2 commits
            user1.SaveCustomer();

            user3.EditCustomer("Alfonso U3 wins");
            user3.SaveCustomer();

            user1.ReleaseCustomer();
            user2.ReleaseCustomer();
            user3.ReleaseCustomer();

            //TEST 2
            // User1 & User2 loads data
            // User1 tryes to edit data - can't
            // User2 releases entity
            // User1 tryes to edit data - succeds
            // User3 tryes to load data - can't
            // User1 saves data
            // User3 tryes to load data - succeds and gets new name
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("SCENARIO 2");

            user1.Initialize(id, "User1");
            user2.Initialize(id, "User2");
            user3.Initialize(id, "User3");

            user1.LoadCustomer();
            user2.LoadCustomer();
            user1.EditCustomer("KaikeU1");
            user2.ReleaseCustomer();
            user1.EditCustomer("KaikeU1");
            user3.LoadCustomer();
            user1.SaveCustomer();
            user3.ReleaseCustomer();
            user1.SaveCustomer();
            user3.Initialize(id, "User3");
            user3.LoadCustomer();
            Console.WriteLine(string.Format("User3 gets new name: {0}", user3.Entity.FirstName));

            user1.ReleaseCustomer();
            user3.ReleaseCustomer();
        }
    }
}
