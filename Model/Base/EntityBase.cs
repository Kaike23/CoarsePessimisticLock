using System;

namespace Model.Base
{
    using Infrastructure.Domain;
    using Session;

    public abstract class EntityBase : IEntity
    {
        private VersionLock _versionLock;

        public EntityBase(Guid id, Guid versionId)
        {
            Id = id;
            VersionId = versionId;
        }

        public VersionLock GetVersion()
        {
            if (_versionLock == null)
                _versionLock = VersionLock.Create();
            return _versionLock;
        }

        public void LoadVersion(Guid versionId)
        {
            _versionLock = VersionLock.Find(versionId);
        }

        public Guid Id { get; private set; }
        public Guid VersionId { get; private set; }
        public VersionLock Version { get { return _versionLock; } }
    }
}
