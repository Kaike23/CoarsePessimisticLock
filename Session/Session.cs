using System;
using System.Data;
using System.Data.SqlClient;

namespace Session
{
    using Infrastructure.Identity;
    using Infrastructure.Lock;
    using Infrastructure.Session;

    public class Session : ISession
    {
        private IdentityMap map;

        public Session(string name, string connectionInfo)
        {
            this.Id = Guid.NewGuid();
            this.Name = name;
            this.map = new IdentityMap();
            this.DbInfo = new DbSessionInfo(connectionInfo);
            this.LockManager = new LockManager();
        }

        #region ISession implementation

        public Guid Id { get; private set; }

        public string Name { get; private set; }

        public IDbSessionInfo DbInfo { get; private set; }

        public ILockManager LockManager { get; private set; }

        public IdentityMap GetIdentityMap()
        {
            return this.map;
        }

        public void Close()
        {
            this.map.Clear();
        }

        #endregion

        private sealed class DbSessionInfo : IDbSessionInfo
        {
            private IDbTransaction transaction;

            public DbSessionInfo(string info)
            {
                this.Connection = new SqlConnection(info);
            }

            public IDbConnection Connection { get; private set; }

            public IDbTransaction Transaction
            {
                get { return this.transaction; }
                set
                {
                    if (this.transaction != null && value != null)
                        throw new InvalidOperationException();
                    this.transaction = value;
                }
            }
        }
    }
}
