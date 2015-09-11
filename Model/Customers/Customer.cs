using System;

namespace Model.Customers
{
    using Infrastructure.Lock;
    using Model.Base;
    using Session;

    public class Customer : EntityBase
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }

        public string FullName { get { return string.Format("{0} {1}", FirstName, LastName); } }

        public Customer(Guid id, Guid versionId, string firstName, string lastName)
            : base(id, versionId)
        {
            FirstName = firstName;
            LastName = lastName;
        }

        public static Customer Create(string firstName, string lastName)
        {
            var sessionManager = SessionManager.Manager;
            var session = sessionManager.GetSession(sessionManager.Current);
            var customer = new Customer(Guid.NewGuid(), Guid.Empty, firstName, lastName);
            return customer;
        }
    }
}