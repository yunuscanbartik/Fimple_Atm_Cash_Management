using Fimple.Atm.Domain.Constants;
using Fimple.Atm.Domain.Entities;
using Fimple.Atm.Domain.Enums;
using Fimple.Atm.Domain.Exceptions;
using Fimple.Atm.Domain.Interfaces;
using Fimple.Atm.Domain.Models;

namespace Fimple.Atm.Application.Services
{
    public class AtmService : IAtmService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAtmRepository _atmRepository;
        private readonly IReconciliationRepository _reconciliationRepository;
        private readonly IQuoteReservationReleaser _quoteReservationReleaser;
        private readonly TimeProvider _timeProvider;

        public AtmService(
            IUnitOfWork unitOfWork,
            IAtmRepository atmRepository,
            IReconciliationRepository reconciliationRepository,
            IQuoteReservationReleaser quoteReservationReleaser,
            TimeProvider timeProvider)
        {
            _unitOfWork = unitOfWork;
            _atmRepository = atmRepository;
            _reconciliationRepository = reconciliationRepository;
            _quoteReservationReleaser = quoteReservationReleaser;
            _timeProvider = timeProvider;
        }

        public async Task<AtmModel.Get.Return> GetAtm(AtmModel.Get.Request request)
        {
            var atms = await _atmRepository.GetAllReadOnly();
            var items = atms.Select(ToAtmItem).ToList();
            return new AtmModel.Get.Return { Items = items };
        }

        public async Task<AtmModel.GetDetail.Return> GetAtmDetail(AtmModel.GetDetail.Request request)
        {
            var atm = await _atmRepository.FindWithCassettesReadOnly(request.AtmId);
            if (atm == null)
            {
                throw new NotFoundException(ErrorCodes.ATM_NOT_FOUND, $"ATM not found: {request.AtmId}");
            }

            return AtmModelMapper.ToGetDetailReturn(atm);
        }

        public async Task<AtmModel.Add.Return> AddAtm(AtmModel.Add.Request request)
        {
            var name = request.Name.Trim();
            var nameExists = await _atmRepository.NameExists(name);
            if (nameExists)
            {
                throw new ConflictException(ErrorCodes.ATM_NAME_ALREADY_EXISTS, $"An ATM named '{name}' already exists.");
            }

            var createdAt = _timeProvider.GetUtcNow().UtcDateTime;
            var atm = new AtmMachine
            {
                Id = Guid.NewGuid(),
                Name = name,
                Status = AtmStatus.Unconfigured,
                CreatedAt = createdAt
            };
            _atmRepository.Add(atm);
            await _unitOfWork.SaveChanges();

            return new AtmModel.Add.Return
            {
                Id = atm.Id,
                Name = atm.Name,
                Status = atm.Status
            };
        }

        public async Task<AtmModel.PutInService.Return> PutInService(AtmModel.PutInService.Request request)
        {
            await using var transaction = await _unitOfWork.BeginTransaction();
            var atm = await _atmRepository.LoadForUpdate(request.AtmId);

            if (!AtmStatusRules.CanPutInService(atm.Status))
            {
                throw new ConflictException(ErrorCodes.INVALID_ATM_STATE,
                    $"An ATM in status {atm.Status} cannot be put in service. It must be {AtmStatus.Loaded} or {AtmStatus.Maintenance}.");
            }

            await EnsureNoReconciliationAwaitingApproval(atm);

            atm.Status = AtmStatus.InService;
            atm.UpdatedAt = _timeProvider.GetUtcNow().UtcDateTime;
            await _unitOfWork.SaveChanges();
            await transaction.Commit();

            return new AtmModel.PutInService.Return
            {
                Id = atm.Id,
                Name = atm.Name,
                Status = atm.Status
            };
        }

        public async Task<AtmModel.EnterMaintenance.Return> EnterMaintenance(AtmModel.EnterMaintenance.Request request)
        {
            await using var transaction = await _unitOfWork.BeginTransaction();
            var atm = await _atmRepository.LoadForUpdate(request.AtmId);

            if (!AtmStatusRules.CanEnterMaintenance(atm.Status))
            {
                throw new ConflictException(ErrorCodes.INVALID_ATM_STATE,
                    $"An ATM in status {atm.Status} cannot enter maintenance. It must be {AtmStatus.InService}.");
            }

            await _quoteReservationReleaser.ReleasePendingQuotes(atm, QuoteStatus.Cancelled);

            atm.Status = AtmStatus.Maintenance;
            atm.UpdatedAt = _timeProvider.GetUtcNow().UtcDateTime;
            await _unitOfWork.SaveChanges();
            await transaction.Commit();

            return new AtmModel.EnterMaintenance.Return
            {
                Id = atm.Id,
                Name = atm.Name,
                Status = atm.Status
            };
        }

        private static AtmModel.AtmItem ToAtmItem(AtmMachine atm)
        {
            return new AtmModel.AtmItem
            {
                Id = atm.Id,
                Name = atm.Name,
                Status = atm.Status
            };
        }

        private async Task EnsureNoReconciliationAwaitingApproval(AtmMachine atm)
        {
            var hasPendingReconciliation = await _reconciliationRepository.HasAwaitingApproval(atm.Id);
            if (hasPendingReconciliation)
            {
                throw new ConflictException(ErrorCodes.RECONCILIATION_AWAITING_APPROVAL,
                    "The ATM cannot return to service until a different user approves the pending reconciliation.");
            }
        }
    }
}
