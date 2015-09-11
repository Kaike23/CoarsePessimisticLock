using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Session
{
    using Infrastructure.Session;

    public class VersionLock
    {
        public static Dictionary<VersionLock, string> Versions = new Dictionary<VersionLock, string>();

        private bool locked;
        private bool isNew;
        private static string UPDATE_SQL = "UPDATE VersionLock SET Value = @NewValue, ModifiedBy = @ModifiedBy, Modified = @Modified WHERE Id = @Id AND Value = @OldValue";
        private static string DELETE_SQL = "DELETE  FROM VersionLock WHERE Id = @Id AND Value = @Value";
        private static string INSERT_SQL = "INSERT INTO VersionLock VALUES (@Id, @Value, @CreatedBy, @Created, @ModifiedBy, @Modified)";
        private static string LOAD_SQL = "SELECT Id, Value, CreatedBy, Created, ModifiedBy, Modified FROM VersionLock WHERE Id = @Id";

        public static VersionLock Find(Guid id)
        {
            VersionLock version = null;
            if (Versions.Values.Count != 0)
                version = Versions.Keys.FirstOrDefault(x => x.Id == id);
            if (version == null)
            {
                version = Load(id);
            }
            return version;
        }
        private static VersionLock Load(Guid id)
        {
            VersionLock versionLock = null;
            try
            {
                using (var selectCommand = new SqlCommand(LOAD_SQL, Connection))
                {
                    selectCommand.Parameters.AddWithValue("@Id", id);
                    var reader = selectCommand.ExecuteReader();
                    if (reader.HasRows)
                    {
                        reader.Read();
                        var value = reader.GetInt64(1);
                        var createdBy = reader.GetString(2);
                        var created = reader.GetDateTime(3);
                        var modifiedBy = reader.GetString(4);
                        var modified = reader.GetDateTime(5);
                        versionLock = new VersionLock(id, value, createdBy, created, modifiedBy, modified);
                        Versions.Add(versionLock, Session.Name);
                    }
                    else
                    {
                        throw new SystemException(string.Format("Version {0} not found.", id));
                    }
                    reader.Close();
                }
            }
            catch (SqlException sqlEx)
            {
                throw new SystemException("unexpected sql error loading version", sqlEx);
            }
            return versionLock;
        }
        public static VersionLock Create()
        {
            VersionLock version = new VersionLock(Guid.NewGuid(), 0, Session.Name, DateTime.Now, Session.Name, DateTime.Now);
            version.isNew = true;
            return version;
        }
        private VersionLock(Guid id, long value, string createdBy, DateTime created, string modifiedBy, DateTime modified)
        {
            Id = id;
            Value = value;
            CreatedBy = createdBy;
            Created = created;
            ModifiedBy = modifiedBy;
            Modified = modified;
        }

        public Guid Id { get; private set; }
        public long Value { get; private set; }
        public string CreatedBy { get; private set; }
        public DateTime Created { get; private set; }
        public string ModifiedBy { get; private set; }
        public DateTime Modified { get; private set; }

        public void Insert()
        {
            if (isNew)
            {
                try
                {
                    var insertCommand = new SqlCommand(INSERT_SQL, Connection, Transaction);
                    insertCommand.Parameters.AddWithValue("@Id", Id);
                    insertCommand.Parameters.AddWithValue("@Value", Value);
                    insertCommand.Parameters.AddWithValue("@CreateBy", CreatedBy);
                    insertCommand.Parameters.AddWithValue("@Created", Created);
                    insertCommand.Parameters.AddWithValue("@ModifiedBy", ModifiedBy);
                    insertCommand.Parameters.AddWithValue("@Modified", Modified);
                    insertCommand.ExecuteNonQuery();
                    Versions.Add(this, Session.Name);
                    isNew = false;
                }
                catch (SqlException sqlEx)
                {
                    throw new SystemException("unexpected sql error inserting version", sqlEx);
                }
            }
        }
        public void Increment()
        {
            if (!locked)
            {
                try
                {
                    var modified = DateTime.Now;
                    var updateCommand = new SqlCommand(UPDATE_SQL, Connection, Transaction);
                    updateCommand.Parameters.AddWithValue("@NewValue", Value + 1);
                    updateCommand.Parameters.AddWithValue("@ModifiedBy", Session.Name);
                    updateCommand.Parameters.AddWithValue("@Modified", modified);
                    updateCommand.Parameters.AddWithValue("@Id", Id);
                    updateCommand.Parameters.AddWithValue("@OldValue", Value);
                    var rowCount = updateCommand.ExecuteNonQuery();
                    if (rowCount == 0)
                    {
                        ThrowConcurrencyException();
                    }
                    Value++;
                    ModifiedBy = Session.Name;
                    Modified = modified;
                    locked = true;
                }
                catch (SqlException sqlEx)
                {
                    throw new SystemException("unexpected sql error incrementing version", sqlEx);
                }
            }
        }
        public void Delete()
        {
            try
            {
                var deleteCommand = new SqlCommand(DELETE_SQL, Connection, Transaction);
                deleteCommand.Parameters.AddWithValue("@Id", Id);
                deleteCommand.Parameters.AddWithValue("@Value", Value);
                var rowCount = deleteCommand.ExecuteNonQuery();
                if (rowCount == 0)
                {
                    ThrowConcurrencyException();
                }
                Versions.Remove(this);
            }
            catch (SqlException sqlEx)
            {
                throw new SystemException("unexpected sql error incrementing version", sqlEx);
            }
        }
        public void Release()
        {
            locked = false;
        }
        private void ThrowConcurrencyException()
        {
            VersionLock currentVersion = Load(this.Id);
            throw new SystemException(string.Format("version modified by {0} at {1}", currentVersion.ModifiedBy, currentVersion.Modified));
        }

        private static ISession Session
        {
            get
            {
                var sessionManager = SessionManager.Manager;
                return sessionManager.GetSession(sessionManager.Current);
            }
        }
        private static SqlConnection Connection { get { return (SqlConnection)Session.DbInfo.Connection; } }
        private static SqlTransaction Transaction { get { return (SqlTransaction)Session.DbInfo.Transaction; } }

    }
}
