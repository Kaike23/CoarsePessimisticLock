using System;
using System.Data;

namespace Infrastructure.Session
{
    using Infrastructure.Identity;
    using Infrastructure.Lock;

    public interface ISession
    {
        Guid Id { get; }
        string Name { get; }
        IDbSessionInfo DbInfo { get; }
        IdentityMap GetIdentityMap();
        void Close();
        ILockManager LockManager { get; }
    }

    public interface IDbSessionInfo
    {
        IDbConnection Connection { get; }
        IDbTransaction Transaction { get; set; }
    }
}
