using Fimple.Atm.Domain.Constants;
using Fimple.Atm.Domain.Entities;
using Fimple.Atm.Domain.Enums;
using Fimple.Atm.Domain.Exceptions;
using Fimple.Atm.Domain.Interfaces;
using Fimple.Atm.Domain.Models;
using Fimple.Atm.Infrastructure.PostgreSQL;
using Microsoft.EntityFrameworkCore;

namespace Fimple.Atm.Application.Services
{
    public class AtmService : IAtmService
    {
        private readonly AppDbContext _appDbContext;
        private readonly IQuoteReservationReleaser _quoteReservationReleaser;
        private readonly TimeProvider _timeProvider;

        public AtmService(AppDbContext appDbContext, IQuoteReservationReleaser quoteReservationReleaser, TimeProvider timeProvider)
        {
            _appDbContext = appDbContext;
            _quoteReservationReleaser = quoteReservationReleaser;
            _timeProvider = timeProvider;
        }

        public async Task<AtmModel.Get.Return> GetAtm(AtmModel.Get.Request request)
        {
            var items = await _appDbContext.AtmMachines
                .AsNoTracking()
                .OrderBy(atm => atm.Name)
                .Select(atm => new AtmModel.AtmItem
                {
                    Id = atm.Id,
                    Name = atm.Name,
                    Status = atm.Status
                })
                .ToListAsync();
            return new AtmModel.Get.Return { Items = items };
        }

        public async Task<AtmModel.GetDetail.Return> GetAtmDetail(AtmModel.GetDetail.Request request)
        {
            var atm = await _appDbContext.AtmMachines
                .AsNoTracking()
                .Include(atmMachine => atmMachine.Cassettes)
                .ThenInclude(cassette => cassette.Contents)
                .FirstOrDefaultAsync(atmMachine => atmMachine.Id == request.AtmId);
            if (atm == null)
            {
                throw new NotFoundException(ErrorCodes.AtmNotFound, $"ATM not found: {request.AtmId}");
            }

            return AtmModelMapper.ToGetDetailReturn(atm);
        }

        public async Task<AtmModel.Add.Return> AddAtm(AtmModel.Add.Request request)
        {
            var name = request.Name.Trim();
            var nameExists = await _appDbContext.AtmMachines.AnyAsync(atm => atm.Name == name);
            if (nameExists)
            {
                throw new ConflictException(ErrorCodes.AtmNameAlreadyExists, $"An ATM named '{name}' already exists.");
            }

            var atm = new AtmMachine
            {
                Id = Guid.NewGuid(),
                Name = name,
                Status = AtmStatus.Unconfigured,
                CreatedAt = _timeProvider.GetUtcNow().UtcDateTime
            };
            _appDbContext.AtmMachines.Add(atm);
            await _appDbContext.SaveChangesAsync();

            return new AtmModel.Add.Return
            {
                Id = atm.Id,
                Name = atm.Name,
                Status = atm.Status
            };
        }

        public async Task<AtmModel.PutInService.Return> PutInService(AtmModel.PutInService.Request request)
        {
            await using var transaction = await _appDbContext.Database.BeginTransactionAsync();
            var atm = await _appDbContext.LoadAtmForUpdate(request.AtmId);

            if (!AtmStatusRules.CanPutInService(atm.Status))
            {
                throw new ConflictException(ErrorCodes.InvalidAtmState,
                    $"An ATM in status {atm.Status} cannot be put in service. It must be {AtmStatus.Loaded} or {AtmStatus.Maintenance}.");
            }

            await EnsureNoReconciliationAwaitingApproval(atm);

            atm.Status = AtmStatus.InService;
            atm.UpdatedAt = _timeProvider.GetUtcNow().UtcDateTime;
            await _appDbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            return new AtmModel.PutInService.Return
            {
                Id = atm.Id,
                Name = atm.Name,
                Status = atm.Status
            };
        }

        public async Task<AtmModel.EnterMaintenance.Return> EnterMaintenance(AtmModel.EnterMaintenance.Request request)
        {
            await using var transaction = await _appDbContext.Database.BeginTransactionAsync();
            var atm = await _appDbContext.LoadAtmForUpdate(request.AtmId);

            if (!AtmStatusRules.CanEnterMaintenance(atm.Status))
            {
                throw new ConflictException(ErrorCodes.InvalidAtmState,
                    $"An ATM in status {atm.Status} cannot enter maintenance. It must be {AtmStatus.InService}.");
            }

            await _quoteReservationReleaser.ReleasePendingQuotes(atm, QuoteStatus.Cancelled);

            atm.Status = AtmStatus.Maintenance;
            atm.UpdatedAt = _timeProvider.GetUtcNow().UtcDateTime;
            await _appDbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            return new AtmModel.EnterMaintenance.Return
            {
                Id = atm.Id,
                Name = atm.Name,
                Status = atm.Status
            };
        }

        private async Task EnsureNoReconciliationAwaitingApproval(AtmMachine atm)
        {
            var hasPendingReconciliation = await _appDbContext.Reconciliations.AnyAsync(reconciliation =>
                reconciliation.AtmId == atm.Id && reconciliation.Status == ReconciliationStatus.AwaitingApproval);
            if (hasPendingReconciliation)
            {
                throw new ConflictException(ErrorCodes.ReconciliationAwaitingApproval,
                    "The ATM cannot return to service until a different user approves the pending reconciliation.");
            }
        }
    }
}
