using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Session
{
    using Infrastructure.Session;

    public sealed class SessionManager : ISessionManager
    {
        private static readonly ISessionManager manager = new SessionManager();

        private static readonly string SQL_CONNECTION = @"Data Source=(LocalDB)\v11.0;AttachDbFilename=D:\GitHub\CoarsePessimisticLock\TestApp\Data\TestDB.mdf;Integrated Security=True;Connect Timeout=30";

        public static ISessionManager Manager { get { return manager; } }

        private Dictionary<Guid, ISession> sessions;

        private SessionManager()
        {
            this.sessions = new Dictionary<Guid, ISession>();
        }

        #region ISessionManager implementation

        public Guid Current { get; set; }

        public Guid Open(string name)
        {
            var session = new Session(name, SQL_CONNECTION);
            this.sessions.Add(session.Id, session);
            return session.Id;
        }

        public ISession GetSession(Guid sessionId)
        {
            Debug.Assert(this.sessions.ContainsKey(sessionId));
            return this.sessions[sessionId];
        }

        public void Close(Guid sessionId)
        {
            Debug.Assert(this.sessions.ContainsKey(sessionId));
            this.sessions[sessionId].Close();
            this.sessions.Remove(sessionId);
        }

        #endregion
    }
}
