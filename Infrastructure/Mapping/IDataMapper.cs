using System;
using System.Collections.Generic;

namespace Infrastructure.Mapping
{
    using Infrastructure.Domain;

    public interface IDataMapper<T> where T : IEntity
    {
        T Find(Guid Id);
        List<T> FindMany(IStatementSource source);

        Guid Insert(T entity);
        void Update(T entity);
        void Delete(T entity);
    }
}