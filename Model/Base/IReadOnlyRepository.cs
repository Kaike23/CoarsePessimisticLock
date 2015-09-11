using System;
using System.Collections.Generic;

namespace Model.Base
{
    using Infrastructure.Domain;

    public interface IReadOnlyRepository<T> where T : IEntity
    {
        T FindBy(Guid id);
        IEnumerable<T> FindAll();
    }
}

