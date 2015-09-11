namespace Model.Base
{
    using Infrastructure.Domain;

    public interface IRepository<T> : IReadOnlyRepository<T>
        where T : IEntity
    {
        void Add(T entity);
        void Update(T entity);
        void Remove(T entity);
    }
}
