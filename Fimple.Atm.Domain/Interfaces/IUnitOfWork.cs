namespace Fimple.Atm.Domain.Interfaces
{
    public interface IUnitOfWork
    {
        public Task<IDatabaseTransaction> BeginTransaction();
        public Task<IDatabaseTransaction> BeginRepeatableReadTransaction();
        public Task SaveChanges();
        public void ClearTracking();
    }
}
