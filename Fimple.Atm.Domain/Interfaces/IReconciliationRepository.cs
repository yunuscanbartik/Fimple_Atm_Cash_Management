using Fimple.Atm.Domain.Entities;

namespace Fimple.Atm.Domain.Interfaces
{
    public interface IReconciliationRepository
    {
        public Task<List<Reconciliation>> GetAllReadOnly(Guid atmId);
        public Task<Reconciliation?> Find(Guid atmId, Guid reconciliationId);
        public Task<Reconciliation?> FindReadOnly(Guid atmId, Guid reconciliationId);
        public Task<bool> HasAwaitingApproval(Guid atmId);
        public void Add(Reconciliation reconciliation);
    }
}
