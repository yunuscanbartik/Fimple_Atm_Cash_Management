using Fimple.Atm.Domain.Entities;
using Fimple.Atm.Domain.Enums;
using Fimple.Atm.Domain.Interfaces;
using Fimple.Atm.Infrastructure.PostgreSQL;
using Microsoft.EntityFrameworkCore;

namespace Fimple.Atm.Infrastructure.Repositories
{
    public class ReconciliationRepository : IReconciliationRepository
    {
        private readonly AppDbContext _appDbContext;

        public ReconciliationRepository(AppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }

        public async Task<List<Reconciliation>> GetAllReadOnly(Guid atmId)
        {
            return await _appDbContext.Reconciliations
                .AsNoTracking()
                .Include(reconciliation => reconciliation.Lines)
                .Where(reconciliation => reconciliation.AtmId == atmId)
                .OrderByDescending(reconciliation => reconciliation.CreatedAt)
                .ToListAsync();
        }

        public async Task<Reconciliation?> Find(Guid atmId, Guid reconciliationId)
        {
            return await _appDbContext.Reconciliations
                .Include(reconciliation => reconciliation.Lines)
                .FirstOrDefaultAsync(reconciliation => reconciliation.Id == reconciliationId && reconciliation.AtmId == atmId);
        }

        public async Task<Reconciliation?> FindReadOnly(Guid atmId, Guid reconciliationId)
        {
            return await _appDbContext.Reconciliations
                .AsNoTracking()
                .Include(reconciliation => reconciliation.Lines)
                .FirstOrDefaultAsync(reconciliation => reconciliation.Id == reconciliationId && reconciliation.AtmId == atmId);
        }

        public async Task<bool> HasAwaitingApproval(Guid atmId)
        {
            return await _appDbContext.Reconciliations.AnyAsync(reconciliation =>
                reconciliation.AtmId == atmId && reconciliation.Status == ReconciliationStatus.AwaitingApproval);
        }

        public void Add(Reconciliation reconciliation)
        {
            _appDbContext.Reconciliations.Add(reconciliation);
        }
    }
}
