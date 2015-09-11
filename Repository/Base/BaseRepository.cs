using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace Repository.Base
{
    using Infrastructure.Domain;
    using Infrastructure.Mapping;
    using Infrastructure.UnitOfWork;
    using Model.Base;

    public abstract class BaseRepository<T> : IRepository<T>, IUnitOfWorkRepository
        where T : EntityBase
    {
        private IUnitOfWork _uow;

        public BaseRepository(IUnitOfWork uow, IDataMapper<T> dataMapper)
        {
            _uow = uow;
            DataMapper = dataMapper;
        }

        #region IRepository

        public void Add(T entity)
        {
            _uow.RegisterNew(entity, this);
        }
        public void Update(T entity)
        {
            _uow.RegisterDirty(entity, this);
        }

        public void Remove(T entity)
        {
            _uow.RegisterRemoved(entity, this);
        }
        public T FindBy(Guid id)
        {
            return DataMapper.Find(id);
        }
        public IEnumerable<T> FindAll()
        {
            return DataMapper.FindMany(new FindAllStatement(TableName));
        }

        #endregion

        public IDataMapper<T> DataMapper { get; private set; }

        protected abstract string TableName { get; }

        private class FindAllStatement : IStatementSource
        {
            public FindAllStatement(string query)
            {
                Query = query;
            }
            public List<IDbDataParameter> Parameters { get { return new List<IDbDataParameter>(); } }
            public string Query { get; private set; }
        }

        #region IUnitOfWorkRepository

        void IUnitOfWorkRepository.PersistCreationOf(IEntity entity) { DataMapper.Insert((T)entity); }
        void IUnitOfWorkRepository.PersistUpdateOf(IEntity entity) { DataMapper.Update((T)entity); }
        void IUnitOfWorkRepository.PersistDeletionOf(IEntity entity) { DataMapper.Delete((T)entity); }

        #endregion
    }
}
