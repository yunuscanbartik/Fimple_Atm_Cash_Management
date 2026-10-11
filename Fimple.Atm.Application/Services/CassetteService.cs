using Fimple.Atm.Domain.Constants;
using Fimple.Atm.Domain.Entities;
using Fimple.Atm.Domain.Enums;
using Fimple.Atm.Domain.Exceptions;
using Fimple.Atm.Domain.Interfaces;
using Fimple.Atm.Domain.Models;
using Fimple.Atm.Infrastructure.PostgreSQL;

namespace Fimple.Atm.Application.Services
{
    public class CassetteService : ICassetteService
    {
        private readonly AppDbContext _appDbContext;
        private readonly ICassetteMovementRecorder _cassetteMovementRecorder;
        private readonly TimeProvider _timeProvider;

        public CassetteService(AppDbContext appDbContext, ICassetteMovementRecorder cassetteMovementRecorder, TimeProvider timeProvider)
        {
            _appDbContext = appDbContext;
            _cassetteMovementRecorder = cassetteMovementRecorder;
            _timeProvider = timeProvider;
        }

        public async Task<AtmModel.AtmDetail> ConfigureCassettes(AtmModel.ConfigureCassettes.Request request)
        {
            CassetteConfigurationValidator.Validate(request.Cassettes);

            await using var transaction = await _appDbContext.Database.BeginTransactionAsync();
            var atm = await _appDbContext.LoadAtmForUpdate(request.AtmId);
            if (!AtmStatusRules.CanConfigureCassettes(atm.Status))
            {
                throw new ConflictException(ErrorCodes.InvalidAtmState,
                    $"Cassettes cannot be configured while the ATM is {atm.Status}. Move it to {AtmStatus.Maintenance} first.");
            }

            var now = _timeProvider.GetUtcNow().UtcDateTime;
            foreach (var cassetteConfiguration in request.Cassettes.OrderBy(configuration => configuration.Position))
            {
                ApplyCassetteConfiguration(atm, cassetteConfiguration, now);
            }

            if (atm.Status == AtmStatus.Unconfigured)
            {
                atm.Status = AtmStatus.Configured;
            }

            atm.UpdatedAt = now;
            await _appDbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            return AtmModelMapper.ToAtmDetail(atm);
        }

        public async Task<AtmModel.AtmDetail> LoadCash(AtmModel.LoadCash.Request request)
        {
            await using var transaction = await _appDbContext.Database.BeginTransactionAsync();
            var atm = await _appDbContext.LoadAtmForUpdate(request.AtmId);
            if (!AtmStatusRules.CanLoadCash(atm.Status))
            {
                throw new ConflictException(ErrorCodes.InvalidAtmState,
                    $"Cash cannot be loaded while the ATM is {atm.Status}. Configure its cassettes first, or move it to {AtmStatus.Maintenance}.");
            }

            foreach (var item in request.Items)
            {
                LoadCashItem(atm, item, request.UserName);
            }

            if (atm.Status == AtmStatus.Configured)
            {
                atm.Status = AtmStatus.Loaded;
            }

            atm.UpdatedAt = _timeProvider.GetUtcNow().UtcDateTime;
            await _appDbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            return AtmModelMapper.ToAtmDetail(atm);
        }

        public async Task<AtmModel.AtmDetail> UnloadCash(AtmModel.UnloadCash.Request request)
        {
            await using var transaction = await _appDbContext.Database.BeginTransactionAsync();
            var atm = await _appDbContext.LoadAtmForUpdate(request.AtmId);
            if (!AtmStatusRules.CanUnloadCash(atm.Status))
            {
                throw new ConflictException(ErrorCodes.InvalidAtmState,
                    $"Cash cannot be unloaded while the ATM is {atm.Status}. Move it to {AtmStatus.Maintenance} first.");
            }

            foreach (var item in request.Items)
            {
                UnloadCashItem(atm, item, request.UserName);
            }

            atm.UpdatedAt = _timeProvider.GetUtcNow().UtcDateTime;
            await _appDbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            return AtmModelMapper.ToAtmDetail(atm);
        }

        private void ApplyCassetteConfiguration(AtmMachine atm, AtmModel.CassetteConfigItem configuration, DateTime now)
        {
            var denomination = ToDenomination(configuration.Denomination);
            var cassette = atm.Cassettes.FirstOrDefault(existingCassette => existingCassette.Position == configuration.Position);

            if (cassette == null)
            {
                cassette = new Cassette
                {
                    Id = Guid.NewGuid(),
                    AtmId = atm.Id,
                    Position = configuration.Position,
                    Kind = configuration.Kind,
                    Currency = configuration.Currency,
                    Denomination = denomination,
                    Capacity = AtmRules.CassetteCapacity,
                    CreatedAt = now
                };
                atm.Cassettes.Add(cassette);
                AddEmptyRecyclingContent(cassette);
                return;
            }

            var isChanged = cassette.Kind != configuration.Kind
                || cassette.Currency != configuration.Currency
                || cassette.Denomination != denomination;
            if (!isChanged) return;

            var holdsNotes = cassette.Contents.Any(content => content.NoteCount > 0 || content.ReservedCount > 0);
            if (holdsNotes)
            {
                throw new BusinessRuleException(ErrorCodes.CassetteNotEmpty,
                    $"Cassette {cassette.Position} still holds notes and cannot be reconfigured. Unload it first.");
            }

            _appDbContext.CassetteContents.RemoveRange(cassette.Contents);
            cassette.Contents.Clear();
            cassette.Kind = configuration.Kind;
            cassette.Currency = configuration.Currency;
            cassette.Denomination = denomination;
            cassette.UpdatedAt = now;
            AddEmptyRecyclingContent(cassette);
        }

        private void AddEmptyRecyclingContent(Cassette cassette)
        {
            if (cassette.Kind != CassetteKind.Recycling) return;

            _cassetteMovementRecorder.GetOrAddContent(cassette, cassette.Currency.Value, cassette.Denomination.Value);
        }

        private void LoadCashItem(AtmMachine atm, AtmModel.CashItem item, string? userName)
        {
            var cassette = FindCassette(atm, item.CassettePosition);
            var noteType = ResolveNoteType(cassette, item);

            var currentNoteCount = cassette.Contents.Sum(content => content.NoteCount);
            if (currentNoteCount + item.NoteCount > cassette.Capacity)
            {
                throw new BusinessRuleException(ErrorCodes.CassetteCapacityExceeded,
                    $"Cassette {cassette.Position} holds {currentNoteCount} notes and has a capacity of {cassette.Capacity}; " +
                    $"{item.NoteCount} more notes do not fit.");
            }

            var content = _cassetteMovementRecorder.GetOrAddContent(cassette, noteType.Currency, noteType.Denomination);
            _cassetteMovementRecorder.Record(new CassetteMovementRequest
            {
                Cassette = cassette,
                Content = content,
                MovementType = CassetteMovementType.Load,
                NoteCountDelta = item.NoteCount,
                PerformedBy = userName
            });
        }

        private void UnloadCashItem(AtmMachine atm, AtmModel.CashItem item, string? userName)
        {
            var cassette = FindCassette(atm, item.CassettePosition);
            var noteType = ResolveNoteType(cassette, item);

            var content = cassette.Contents.FirstOrDefault(existingContent =>
                existingContent.Currency == noteType.Currency && existingContent.Denomination == noteType.Denomination);
            var availableCount = content?.AvailableCount ?? 0;
            if (content == null || availableCount < item.NoteCount)
            {
                throw new BusinessRuleException(ErrorCodes.InsufficientNotes,
                    $"Cassette {cassette.Position} has {availableCount} available {noteType.Currency} {(int)noteType.Denomination} notes; " +
                    $"{item.NoteCount} were requested.");
            }

            _cassetteMovementRecorder.Record(new CassetteMovementRequest
            {
                Cassette = cassette,
                Content = content,
                MovementType = CassetteMovementType.Unload,
                NoteCountDelta = -item.NoteCount,
                PerformedBy = userName
            });
        }

        private static Cassette FindCassette(AtmMachine atm, int position)
        {
            var cassette = atm.Cassettes.FirstOrDefault(existingCassette => existingCassette.Position == position);
            if (cassette == null)
            {
                throw new NotFoundException(ErrorCodes.CassetteNotFound, $"Cassette {position} is not configured on this ATM.");
            }

            return cassette;
        }

        private static (Currency Currency, Denomination Denomination) ResolveNoteType(Cassette cassette, AtmModel.CashItem item)
        {
            var requestedDenomination = ToDenomination(item.Denomination);

            if (cassette.Kind == CassetteKind.Deposit)
            {
                if (item.Currency == null || requestedDenomination == null)
                {
                    throw new BusinessRuleException(ErrorCodes.NoteTypeRequired,
                        $"Cassette {cassette.Position} is the deposit cassette; currency and denomination are required.");
                }

                return (item.Currency.Value, requestedDenomination.Value);
            }

            var currencyMismatch = item.Currency != null && item.Currency != cassette.Currency;
            var denominationMismatch = requestedDenomination != null && requestedDenomination != cassette.Denomination;
            if (currencyMismatch || denominationMismatch)
            {
                throw new BusinessRuleException(ErrorCodes.CassetteNoteMismatch,
                    $"Cassette {cassette.Position} only holds {cassette.Currency} {(int)cassette.Denomination.Value} notes.");
            }

            return (cassette.Currency.Value, cassette.Denomination.Value);
        }

        private static Denomination? ToDenomination(int? denominationValue)
        {
            return denominationValue.HasValue ? (Denomination)denominationValue.Value : null;
        }
    }
}
