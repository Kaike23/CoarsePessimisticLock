namespace Repository
{
    using Infrastructure.Mapping;
    using Infrastructure.UnitOfWork;
    using Model.Customers;
    using Repository.Base;

    public class CustomerRepository : BaseRepository<Customer>, ICustomerRepository
    {
        public CustomerRepository(IUnitOfWork uow, IDataMapper<Customer> mapper) : base(uow, mapper) { }

        protected override string TableName { get { return "Customers"; } }
    }
}
