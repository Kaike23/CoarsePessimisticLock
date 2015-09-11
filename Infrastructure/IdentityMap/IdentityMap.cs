using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Infrastructure.Identity
{
    using Infrastructure.Domain;

    public class IdentityMap
    {
        private Dictionary<Guid, IEntity> entities;

        public IdentityMap()
        {
            this.entities = new Dictionary<Guid, IEntity>();
        }

        public IEntity Get(Guid key)
        {
            IEntity entity = null;
            this.entities.TryGetValue(key, out entity);
            return entity;
        }

        public void Add(Guid key, IEntity value)
        {
            Debug.Assert(!this.entities.ContainsKey(key));
            this.entities.Add(key, value);
        }

        public void Remove(Guid key)
        {
            this.entities.Remove(key);
        }

        public void Clear()
        {
            this.entities.Clear();
        }
    }
}
