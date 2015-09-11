using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace Repository.Mapping.SQL
{
    using Infrastructure.Mapping;
    using Model.Customers;
    using Repository.Mapping.SQL.Base;

    public class CustomerSQLMapper : BaseSQLMapper<Customer>
    {
        public CustomerSQLMapper() : base() { }

        public static string Columns { get { return "*"; } }
        public static string TableName { get { return "Customers"; } }

        protected override string FindStatement { get { return string.Format("SELECT {0} FROM {1} WHERE Id = @Id", Columns, TableName); } }

        protected override Customer DoLoad(Guid id, Guid versionId, SqlDataReader reader)
        {
            var firstName = reader.GetString(2);
            var lastName = reader.GetString(3);
            var customer = new Customer(id, versionId, firstName, lastName);
            return customer;
        }
        public List<Customer> FindByName(string firstName, string lastName)
        {
            return FindMany(new FindByNameStatement(firstName, lastName));
        }
        private class FindByNameStatement : IStatementSource
        {
            private string _firstName;
            private string _lastName;
            public FindByNameStatement(string firstName, string lastName)
            {
                _firstName = firstName;
                _lastName = lastName;
            }
            public List<IDbDataParameter> Parameters
            {
                get
                {
                    var parameters = new List<IDbDataParameter>();
                    parameters.Add(new SqlParameter("@FirstName", _firstName));
                    parameters.Add(new SqlParameter("@LastName", _lastName));
                    return parameters;
                }
            }
            public string Query
            {
                get
                {
                    return "SELECT " + Columns +
                           " FROM " + TableName +
                           " WHERE UPPER(FirstName) like UPPER(@FisrtName)" +
                           "   AND UPPER(LastName) like UPPER(@LastName)" +
                           " ORDER BY LastName";
                }
            }
        }

        protected override string InsertStatement { get { return string.Format("INSERT INTO {0} VALUES (@Id, @VersionId, @FirstName, @LastName)", TableName); } }
        protected override string UpdateStatement { get { return string.Format("UPDATE {0} SET FirstName = @FirstName, LastName = @LastName WHERE Id = @Id", TableName); } }
        protected override string DeleteStatement { get { return string.Format("DELETE FROM {0} WHERE Id = @Id", TableName); } }
        protected override void DoInsert(Customer entity, SqlCommand insertCommand)
        {
            insertCommand.Parameters.AddWithValue("@FirstName", entity.FirstName);
            insertCommand.Parameters.AddWithValue("@LastName", entity.LastName);
        }
        protected override void DoUpdate(Customer entity, SqlCommand updateCommand)
        {
            updateCommand.Parameters.AddWithValue("@FirstName", entity.FirstName);
            updateCommand.Parameters.AddWithValue("@LastName", entity.LastName);
        }
    }
}
