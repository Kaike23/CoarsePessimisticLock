using System;

namespace Infrastructure.Session
{
    public interface ISessionManager
    {
        Guid Current { get; set; }

        Guid Open(string name);
        ISession GetSession(Guid sessionId);
        void Close(Guid sessionId);
    }
}
