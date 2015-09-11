using System;

namespace Infrastructure.Domain
{
    using Infrastructure.Lock;

    public interface IEntity
    {
        Guid Id { get; }
        Guid VersionId { get; }
        //DateTime Created { get; set; }
        //string CreatedBy { get; set; }
        //DateTime Modified { get; }
        //string ModifiedBy { get; }
        //VersionLock VersionLock { get; }
    }
}
