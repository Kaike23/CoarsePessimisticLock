namespace Infrastructure.UnitOfWork
{
    using Infrastructure.Domain;

    public interface IUnitOfWorkRepository
    {
        void PersistCreationOf(IEntity entity);
        void PersistUpdateOf(IEntity entity);
        void PersistDeletionOf(IEntity entity);
    }
}
