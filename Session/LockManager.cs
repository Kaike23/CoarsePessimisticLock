using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace Session
{
    using Infrastructure.Lock;
    using Infrastructure.Session;

    public class LockManager : ILockManager
    {
        private Dictionary<Guid, LockType> locks;
        private static readonly string CREATE_QUERY = "INSERT INTO Locks VALUES(@LockableId, @OwnerId, @LockType)";
        private static readonly string READ_QUERY = "SELECT OwnerId, LockType FROM Locks WHERE LockableId = @LockableId";
        private static readonly string UPDATE_QUERY = "UPDATE Locks SET LockType = @LockType WHERE LockableId = @LockableId AND OwnerId = @OwnerId";
        private static readonly string DELETE_QUERY = "DELETE FROM Locks WHERE LockableId = @LockableId AND OwnerId = @OwnerId";
        private static readonly string DELETEALL_QUERY = "DELETE FROM Locks WHERE OwnerId = @OwnerId";

        public LockManager()
        {
            this.locks = new Dictionary<Guid, LockType>();
        }

        public bool GetLock(Guid entityId, LockType lockType)
        {
            LockType value;
            if (locks.TryGetValue(entityId, out value) && lockType == value)
                return true;

            var sessionManager = SessionManager.Manager;
            var session = sessionManager.GetSession(sessionManager.Current);
            using (var transaction = session.DbInfo.Connection.BeginTransaction(IsolationLevel.Serializable))
            {
                try
                {
                    var reader = ExecuteReader(entityId, session, (SqlTransaction)transaction);
                    var query = CREATE_QUERY;
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            var sessionId = reader.GetGuid(0);
                            var type = reader.GetInt16(1);

                            if (!Enum.TryParse<LockType>(type.ToString(), out value))
                            {
                                reader.Close();
                                throw new Exception("Wrong LockType fetched. " + type);
                            }

                            if ((lockType == LockType.Write || value == LockType.Write) && session.Id.CompareTo(sessionId) != 0)
                            {
                                reader.Close();
                                throw new Exception(string.Format("Can't get {0} lock for {1}, session {2} has {3} lock.", lockType.ToString(), SessionManager.Manager.GetSession(session.Id).Name, SessionManager.Manager.GetSession(sessionId).Name, value.ToString()));
                            }

                            if (session.Id.CompareTo(sessionId) == 0)
                                query = UPDATE_QUERY;
                        }
                    }
                    reader.Close();
                    ExecuteNonQuery(query, entityId, session, lockType, transaction);
                    transaction.Commit();
                    Console.WriteLine(string.Format("Session {0} adquired {1} lock for VersionId: {2}", session.Name, lockType.ToString(), entityId));
                    if (locks.ContainsKey(entityId))
                        locks[entityId] = lockType;
                    else
                        locks.Add(entityId, lockType);
                }
                catch (Exception ex)
                {

                    transaction.Rollback();
                    Console.WriteLine(ex.Message);
                    return false;
                }
            }
            return true;
        }

        public bool ReleaseLock(Guid entityId)
        {
            if (!locks.ContainsKey(entityId))
                return true;

            var sessionManager = SessionManager.Manager;
            var session = sessionManager.GetSession(sessionManager.Current);
            using (var transaction = session.DbInfo.Connection.BeginTransaction(IsolationLevel.Serializable))
            {
                try
                {
                    ExecuteDelete(entityId, session, transaction);
                    transaction.Commit();
                    Console.WriteLine(string.Format("Session {0} released VersionId: {1}", session.Name, entityId));
                    locks.Remove(entityId);
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Console.WriteLine(ex.Message);
                    return false;
                }
            }
            return true;
        }

        public bool ReleaseAllLocks()
        {
            var sessionManager = SessionManager.Manager;
            var session = sessionManager.GetSession(sessionManager.Current);
            using (var transaction = session.DbInfo.Connection.BeginTransaction(IsolationLevel.Serializable))
            {
                try
                {
                    ExecuteDeleteAll(session, transaction);
                    transaction.Commit();
                    locks.Clear();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Console.WriteLine(ex.Message);
                    return false;
                }
            }
            return true;
        }

        private SqlDataReader ExecuteReader(Guid entityId, ISession session, IDbTransaction transaction)
        {
            var command = new SqlCommand(READ_QUERY, (SqlConnection)session.DbInfo.Connection, (SqlTransaction)transaction);
            command.Parameters.AddWithValue("@LockableId", entityId);
            return command.ExecuteReader();
        }

        private int ExecuteNonQuery(string query, Guid entityId, ISession session, LockType lockType, IDbTransaction transaction)
        {
            var command = new SqlCommand(query, (SqlConnection)session.DbInfo.Connection, (SqlTransaction)transaction);
            command.Parameters.AddWithValue("@LockableId", entityId);
            command.Parameters.AddWithValue("@OwnerId", session.Id);
            command.Parameters.AddWithValue("@LockType", lockType);
            return command.ExecuteNonQuery();
        }

        private int ExecuteDelete(Guid entityId, ISession session, IDbTransaction transaction)
        {
            var command = new SqlCommand(DELETE_QUERY, (SqlConnection)session.DbInfo.Connection, (SqlTransaction)transaction);
            command.Parameters.AddWithValue("@LockableId", entityId);
            command.Parameters.AddWithValue("@OwnerId", session.Id);
            return command.ExecuteNonQuery();
        }

        private int ExecuteDeleteAll(ISession session, IDbTransaction transaction)
        {
            var command = new SqlCommand(DELETEALL_QUERY, (SqlConnection)session.DbInfo.Connection, (SqlTransaction)transaction);
            command.Parameters.AddWithValue("@OwnerId", session.Id);
            return command.ExecuteNonQuery();
        }
    }
}
