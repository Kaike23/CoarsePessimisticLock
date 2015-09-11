using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace Repository.Mapping.SQL.Base
{
    using Infrastructure.Lock;
    using Infrastructure.Mapping;
    using Infrastructure.Session;
    using Model.Base;
    using Session;

    public abstract class BaseSQLMapper<T> : IDataMapper<T>
        where T : EntityBase
    {
        public BaseSQLMapper()
        { }

        protected Dictionary<Guid, T> loadedMap = new Dictionary<Guid, T>();
        protected abstract string FindStatement { get; }
        protected abstract string InsertStatement { get; }
        protected abstract string UpdateStatement { get; }
        protected abstract string DeleteStatement { get; }

        #region IDataMapper

        public T Find(Guid id)
        {
            T result;
            if (loadedMap.TryGetValue(id, out result))
                return result;

            try
            {
                using (var sqlCommand = new SqlCommand(FindStatement, DBConnection))
                {
                    sqlCommand.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = id;
                    var reader = sqlCommand.ExecuteReader();
                    result = Load(reader);
                    reader.Close();
                    if (!Session.LockManager.GetLock(result.VersionId, LockType.Read)) return null;
                    result.LoadVersion(result.VersionId);
                }
                return result;
            }
            catch (SqlException e)
            {
                throw new ApplicationException(e.Message, e);
            }
        }
        public List<T> FindMany(IStatementSource source)
        {
            try
            {
                var sqlCommand = new SqlCommand(source.Query, DBConnection);
                sqlCommand.Parameters.AddRange(source.Parameters.ToArray());
                var reader = sqlCommand.ExecuteReader();
                var result = LoadAll(reader);
                reader.Close();
                return result;
            }
            catch (SqlException e)
            {
                throw new ApplicationException(e.Message, e);
            }
        }
        public Guid Insert(T entity)
        {
            try
            {
                entity.GetVersion().Insert();
                var insertCommand = new SqlCommand(InsertStatement, DBConnection, DBTransaction);
                insertCommand.Parameters.AddWithValue("@Id", entity.Id);
                insertCommand.Parameters.AddWithValue("@VersionId", entity.VersionId);
                DoInsert(entity, insertCommand);

                //Session.LockManager.GetLock(entity.Version.Id, LockType.Write);
                

                var affectedRows = insertCommand.ExecuteNonQuery();
                loadedMap.Add(entity.Id, entity);
                return entity.Id;
            }
            catch (SqlException e)
            {
                throw new ApplicationException(e.Message, e);
            }
        }
        public void Update(T entity)
        {
            try
            {
                var updateCommand = new SqlCommand(UpdateStatement, DBConnection, DBTransaction);
                updateCommand.Parameters.AddWithValue("@Id", entity.Id);
                DoUpdate(entity, updateCommand);

                //Session.LockManager.GetLock(entity.Version.Id, LockType.Write);
                entity.GetVersion().Increment();
                
                var rowCount = updateCommand.ExecuteNonQuery();
                if (rowCount == 0)
                {
                    throw new Exception(string.Format("Concurrency exception on {0}", entity.Id));
                }
            }
            catch (SqlException e)
            {
                throw new ApplicationException(e.Message, e);
            }
        }
        public void Delete(T entity)
        {
            try
            {
                var deleteCommand = new SqlCommand(DeleteStatement, DBConnection, DBTransaction);
                deleteCommand.Parameters.AddWithValue("@Id", entity.Id);

                //Session.LockManager.GetLock(entity.VersionId, LockType.Write);
                entity.GetVersion().Increment();

                deleteCommand.ExecuteNonQuery();
            }
            catch (SqlException e)
            {
                throw new ApplicationException(e.Message, e);
            }
        }

        #endregion

        protected T Load(SqlDataReader reader)
        {
            if (!reader.HasRows) return default(T);
            reader.Read();
            var id = reader.GetGuid(0);
            var versionId = reader.GetGuid(1);
            if (loadedMap.ContainsKey(id)) return loadedMap[id];
            var resultEntity = DoLoad(id, versionId, reader);
            //resultEntity.SetVersion(versionId);
            loadedMap.Add(id, resultEntity);
            return resultEntity;
        }
        protected List<T> LoadAll(SqlDataReader reader)
        {
            var resultEntities = new List<T>();
            if (reader.HasRows)
            {
                while (reader.Read())
                    resultEntities.Add(Load(reader));
            }
            return resultEntities;
        }

        protected abstract T DoLoad(Guid id, Guid versionId, SqlDataReader reader);
        protected abstract void DoInsert(T entity, SqlCommand insertCommand);
        protected abstract void DoUpdate(T entity, SqlCommand updateCommand);

        private SqlConnection DBConnection { get { return (SqlConnection)Session.DbInfo.Connection; } }
        private SqlTransaction DBTransaction { get { return (SqlTransaction)Session.DbInfo.Transaction; } }

        private ISession Session
        {
            get
            {
                var sessionManager = SessionManager.Manager;
                return sessionManager.GetSession(sessionManager.Current);
            }
        }
    }
}
