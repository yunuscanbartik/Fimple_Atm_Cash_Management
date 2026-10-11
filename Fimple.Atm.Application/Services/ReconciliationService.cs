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
    public class ReconciliationService : IReconciliationService
    {
        private readonly AppDbContext _appDbContext;
        private readonly ICassetteMovementRecorder _cassetteMovementRecorder;
        private readonly TimeProvider _timeProvider;

        public ReconciliationService(AppDbContext appDbContext, ICassetteMovementRecorder cassetteMovementRecorder, TimeProvider timeProvider)
        {
            _appDbContext = appDbContext;
            _cassetteMovementRecorder = cassetteMovementRecorder;
            _timeProvider = timeProvider;
        }

        public async Task<ReconciliationModel.Get.Return> GetReconciliation(ReconciliationModel.Get.Request request)
        {
            var cassettes = await LoadCassettes(request.AtmId);
            var reconciliations = await _appDbContext.Reconciliations
                .AsNoTracking()
                .Include(reconciliation => reconciliation.Lines)
                .Where(reconciliation => reconciliation.AtmId == request.AtmId)
                .OrderByDescending(reconciliation => reconciliation.CreatedAt)
                .ToListAsync();

            var items = reconciliations
                .Select(reconciliation => ReconciliationModelMapper.ToReconciliationItem(reconciliation, cassettes))
                .ToList();
            return new ReconciliationModel.Get.Return { Items = items };
        }

        public async Task<ReconciliationModel.ReconciliationItem> GetReconciliationDetail(ReconciliationModel.GetDetail.Request request)
        {
            var cassettes = await LoadCassettes(request.AtmId);
            var reconciliation = await _appDbContext.Reconciliations
                .AsNoTracking()
                .Include(existingReconciliation => existingReconciliation.Lines)
                .FirstOrDefaultAsync(existingReconciliation =>
                    existingReconciliation.Id == request.ReconciliationId && existingReconciliation.AtmId == request.AtmId);
            if (reconciliation == null)
            {
                throw ReconciliationNotFound(request.ReconciliationId);
            }

            return ReconciliationModelMapper.ToReconciliationItem(reconciliation, cassettes);
        }

        public async Task<ReconciliationModel.ReconciliationItem> AddReconciliation(ReconciliationModel.Add.Request request)
        {
            var userName = RequireUserName(request.UserName);

            await using var transaction = await _appDbContext.Database.BeginTransactionAsync();
            var atm = await _appDbContext.LoadAtmForUpdate(request.AtmId);
            if (atm.Status != AtmStatus.Maintenance)
            {
                throw new ConflictException(ErrorCodes.InvalidAtmState,
                    $"Reconciliation is only allowed while the ATM is {AtmStatus.Maintenance}; it is {atm.Status}.");
            }

            await EnsureNoReconciliationAwaitingApproval(atm);

            var countedNotes = ResolveCounts(atm, request.Counts);
            var rates = await LoadExchangeRates();
            var now = _timeProvider.GetUtcNow().UtcDateTime;

            var reconciliation = new Reconciliation
            {
                Id = Guid.NewGuid(),
                AtmId = atm.Id,
                Status = ReconciliationStatus.Completed,
                CreatedBy = userName,
                CreatedAt = now
            };

            var totalDifferenceInBaseCurrency = 0m;
            foreach (var cassette in atm.Cassettes.OrderBy(atmCassette => atmCassette.Position))
            {
                foreach (var (noteType, countedCount) in GetCassetteCounts(cassette, countedNotes))
                {
                    var content = _cassetteMovementRecorder.GetOrAddContent(cassette, noteType.Currency, noteType.Denomination);
                    var difference = countedCount - content.NoteCount;

                    reconciliation.Lines.Add(new ReconciliationLine
                    {
                        Id = Guid.NewGuid(),
                        ReconciliationId = reconciliation.Id,
                        CassetteId = cassette.Id,
                        Currency = noteType.Currency,
                        Denomination = noteType.Denomination,
                        SystemCount = content.NoteCount,
                        CountedCount = countedCount,
                        Difference = difference
                    });

                    totalDifferenceInBaseCurrency += GetBaseCurrencyValue(noteType, difference, rates);
                    RecordAdjustment(cassette, content, difference, reconciliation.Id, userName);
                }
            }

            reconciliation.TotalDifferenceInBaseCurrency = totalDifferenceInBaseCurrency;
            var needsApproval = totalDifferenceInBaseCurrency > ReconciliationRules.ApprovalThresholdInBaseCurrency;
            reconciliation.Status = needsApproval ? ReconciliationStatus.AwaitingApproval : ReconciliationStatus.Completed;
            _appDbContext.Reconciliations.Add(reconciliation);

            atm.UpdatedAt = now;
            await _appDbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            return ReconciliationModelMapper.ToReconciliationItem(reconciliation, atm.Cassettes);
        }

        public async Task<ReconciliationModel.ReconciliationItem> ApproveReconciliation(ReconciliationModel.Approve.Request request)
        {
            var approverName = RequireUserName(request.UserName);

            await using var transaction = await _appDbContext.Database.BeginTransactionAsync();
            var atm = await _appDbContext.LoadAtmForUpdate(request.AtmId);
            var reconciliation = await _appDbContext.Reconciliations
                .Include(existingReconciliation => existingReconciliation.Lines)
                .FirstOrDefaultAsync(existingReconciliation =>
                    existingReconciliation.Id == request.ReconciliationId && existingReconciliation.AtmId == request.AtmId);
            if (reconciliation == null)
            {
                throw ReconciliationNotFound(request.ReconciliationId);
            }

            if (reconciliation.Status != ReconciliationStatus.AwaitingApproval)
            {
                throw new ConflictException(ErrorCodes.ReconciliationNotPending,
                    $"Only a reconciliation that is awaiting approval can be approved; this one is {reconciliation.Status}.");
            }

            var isSameUser = string.Equals(reconciliation.CreatedBy, approverName, StringComparison.OrdinalIgnoreCase);
            if (isSameUser)
            {
                throw new ForbiddenException(ErrorCodes.FourEyesViolation,
                    "A reconciliation must be approved by a different user than the one who entered the counts.");
            }

            var now = _timeProvider.GetUtcNow().UtcDateTime;
            reconciliation.Status = ReconciliationStatus.Approved;
            reconciliation.ApprovedBy = approverName;
            reconciliation.ApprovedAt = now;
            atm.UpdatedAt = now;

            await _appDbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            return ReconciliationModelMapper.ToReconciliationItem(reconciliation, atm.Cassettes);
        }

        private async Task<List<Cassette>> LoadCassettes(Guid atmId)
        {
            var atmExists = await _appDbContext.AtmMachines.AnyAsync(atm => atm.Id == atmId);
            if (!atmExists)
            {
                throw new NotFoundException(ErrorCodes.AtmNotFound, $"ATM not found: {atmId}");
            }

            return await _appDbContext.Cassettes.AsNoTracking().Where(cassette => cassette.AtmId == atmId).ToListAsync();
        }

        private static NotFoundException ReconciliationNotFound(Guid reconciliationId)
        {
            return new NotFoundException(ErrorCodes.ReconciliationNotFound, $"Reconciliation not found: {reconciliationId}");
        }

        private static string RequireUserName(string? userName)
        {
            if (string.IsNullOrWhiteSpace(userName))
            {
                throw new RequestValidationException(ErrorCodes.UserHeaderRequired, "The X-User header is required for this operation.");
            }

            return userName;
        }

        private async Task EnsureNoReconciliationAwaitingApproval(AtmMachine atm)
        {
            var hasPendingReconciliation = await _appDbContext.Reconciliations.AnyAsync(reconciliation =>
                reconciliation.AtmId == atm.Id && reconciliation.Status == ReconciliationStatus.AwaitingApproval);
            if (hasPendingReconciliation)
            {
                throw new ConflictException(ErrorCodes.ReconciliationAwaitingApproval,
                    "A previous reconciliation for this ATM is still awaiting approval.");
            }
        }

        private static Dictionary<(Guid CassetteId, Currency Currency, Denomination Denomination), int> ResolveCounts(
            AtmMachine atm,
            List<ReconciliationModel.CountItem> counts)
        {
            var countedNotes = new Dictionary<(Guid CassetteId, Currency Currency, Denomination Denomination), int>();

            foreach (var countItem in counts)
            {
                var cassette = atm.Cassettes.FirstOrDefault(atmCassette => atmCassette.Position == countItem.CassettePosition);
                if (cassette == null)
                {
                    throw new NotFoundException(ErrorCodes.CassetteNotFound, $"Cassette {countItem.CassettePosition} is not configured on this ATM.");
                }

                var noteType = CassetteNoteTypeResolver.Resolve(cassette, countItem.Currency, CassetteNoteTypeResolver.ToDenomination(countItem.Denomination));
                var key = (cassette.Id, noteType.Currency, noteType.Denomination);
                if (!countedNotes.TryAdd(key, countItem.CountedNoteCount))
                {
                    throw new BusinessRuleException(ErrorCodes.InvalidReconciliationCounts,
                        $"Cassette {cassette.Position} has more than one count for {noteType.Currency} {(int)noteType.Denomination}.");
                }
            }

            EnsureEveryRecyclingCassetteCounted(atm, countedNotes);
            EnsureCountsFitCapacity(atm, countedNotes);
            return countedNotes;
        }

        private static void EnsureEveryRecyclingCassetteCounted(
            AtmMachine atm,
            Dictionary<(Guid CassetteId, Currency Currency, Denomination Denomination), int> countedNotes)
        {
            var uncountedCassette = atm.Cassettes
                .Where(cassette => cassette.Kind == CassetteKind.Recycling)
                .FirstOrDefault(cassette => !countedNotes.Keys.Any(key => key.CassetteId == cassette.Id));
            if (uncountedCassette != null)
            {
                throw new BusinessRuleException(ErrorCodes.InvalidReconciliationCounts,
                    $"A count is required for recycling cassette {uncountedCassette.Position}.");
            }
        }

        private static void EnsureCountsFitCapacity(
            AtmMachine atm,
            Dictionary<(Guid CassetteId, Currency Currency, Denomination Denomination), int> countedNotes)
        {
            foreach (var cassette in atm.Cassettes)
            {
                var countedTotal = countedNotes.Where(pair => pair.Key.CassetteId == cassette.Id).Sum(pair => pair.Value);
                if (countedTotal > cassette.Capacity)
                {
                    throw new BusinessRuleException(ErrorCodes.InvalidReconciliationCounts,
                        $"Cassette {cassette.Position} cannot hold {countedTotal} notes; its capacity is {cassette.Capacity}.");
                }
            }
        }

        private static List<((Currency Currency, Denomination Denomination) NoteType, int CountedCount)> GetCassetteCounts(
            Cassette cassette,
            Dictionary<(Guid CassetteId, Currency Currency, Denomination Denomination), int> countedNotes)
        {
            var noteTypes = cassette.Contents
                .Select(content => (content.Currency, content.Denomination))
                .Union(countedNotes.Keys.Where(key => key.CassetteId == cassette.Id).Select(key => (key.Currency, key.Denomination)))
                .OrderBy(noteType => noteType.Currency)
                .ThenBy(noteType => noteType.Denomination)
                .ToList();

            return noteTypes
                .Select(noteType => (noteType, countedNotes.GetValueOrDefault((cassette.Id, noteType.Currency, noteType.Denomination))))
                .ToList();
        }

        private async Task<Dictionary<Currency, decimal>> LoadExchangeRates()
        {
            return await _appDbContext.ExchangeRates
                .AsNoTracking()
                .ToDictionaryAsync(exchangeRate => exchangeRate.Currency, exchangeRate => exchangeRate.RateToBaseCurrency);
        }

        private static decimal GetBaseCurrencyValue((Currency Currency, Denomination Denomination) noteType, int difference, Dictionary<Currency, decimal> rates)
        {
            if (!rates.TryGetValue(noteType.Currency, out var rate))
            {
                throw new BusinessRuleException(ErrorCodes.ExchangeRateMissing, $"No exchange rate is configured for {noteType.Currency}.");
            }

            return Math.Abs(difference) * (int)noteType.Denomination * rate;
        }

        private void RecordAdjustment(Cassette cassette, CassetteContent content, int difference, Guid reconciliationId, string userName)
        {
            if (difference == 0) return;

            _cassetteMovementRecorder.Record(new CassetteMovementRequest
            {
                Cassette = cassette,
                Content = content,
                MovementType = CassetteMovementType.ReconciliationAdjustment,
                NoteCountDelta = difference,
                ReferenceId = reconciliationId,
                PerformedBy = userName
            });
        }
    }
}
