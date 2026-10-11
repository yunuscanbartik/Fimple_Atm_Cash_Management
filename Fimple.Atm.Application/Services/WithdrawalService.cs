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
    public class WithdrawalService : IWithdrawalService
    {
        private readonly AppDbContext _appDbContext;
        private readonly ICassetteMovementRecorder _cassetteMovementRecorder;
        private readonly IQuoteReservationReleaser _quoteReservationReleaser;
        private readonly ICardHasher _cardHasher;
        private readonly TimeProvider _timeProvider;

        public WithdrawalService(
            AppDbContext appDbContext,
            ICassetteMovementRecorder cassetteMovementRecorder,
            IQuoteReservationReleaser quoteReservationReleaser,
            ICardHasher cardHasher,
            TimeProvider timeProvider)
        {
            _appDbContext = appDbContext;
            _cassetteMovementRecorder = cassetteMovementRecorder;
            _quoteReservationReleaser = quoteReservationReleaser;
            _cardHasher = cardHasher;
            _timeProvider = timeProvider;
        }

        public async Task<WithdrawalModel.CreateQuote.Return> CreateQuote(WithdrawalModel.CreateQuote.Request request)
        {
            var cardHash = _cardHasher.Hash(request.CardNumber);
            var maskedCardNumber = _cardHasher.Mask(request.CardNumber);

            await using var transaction = await _appDbContext.Database.BeginTransactionAsync();
            var atm = await _appDbContext.LoadAtmForUpdate(request.AtmId);
            EnsureInService(atm, "quote a withdrawal");

            await _quoteReservationReleaser.ReleaseExpiredQuotes(atm);
            await _appDbContext.LockCardForUpdate(cardHash);

            var now = _timeProvider.GetUtcNow().UtcDateTime;
            var remainingLimit = await GetRemainingDailyLimit(cardHash, request.Currency, now);
            if (request.Amount > remainingLimit)
            {
                throw new BusinessRuleException(ErrorCodes.DAILY_LIMIT_EXCEEDED,
                    $"The daily {request.Currency} withdrawal limit is {WithdrawalRules.DailyLimits[request.Currency]}; " +
                    $"{remainingLimit} remains for this card today.");
            }

            var selection = NoteSelector.Select(BuildNoteStocks(atm, request.Currency), request.Amount);
            var offeredBreakdowns = GetOfferedBreakdowns(selection, remainingLimit);
            if (offeredBreakdowns.Count == 0)
            {
                throw new BusinessRuleException(ErrorCodes.INSUFFICIENT_CASH,
                    $"This ATM cannot pay out any {request.Currency} amount close to {request.Amount} right now.");
            }

            var quote = BuildQuote(atm, request, cardHash, maskedCardNumber, offeredBreakdowns, now);
            _appDbContext.WithdrawalQuotes.Add(quote);
            ReserveNotes(atm, quote);

            await _appDbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            return WithdrawalModelMapper.ToCreateQuoteReturn(quote, atm.Cassettes, now);
        }

        public async Task<WithdrawalModel.GetQuote.Return> GetQuote(WithdrawalModel.GetQuote.Request request)
        {
            var quote = await _appDbContext.WithdrawalQuotes
                .AsNoTracking()
                .Include(withdrawalQuote => withdrawalQuote.Options)
                .ThenInclude(option => option.Items)
                .FirstOrDefaultAsync(withdrawalQuote => withdrawalQuote.Id == request.QuoteId && withdrawalQuote.AtmId == request.AtmId);
            if (quote == null)
            {
                throw QuoteNotFound(request.QuoteId);
            }

            var cassettes = await _appDbContext.Cassettes
                .AsNoTracking()
                .Where(cassette => cassette.AtmId == request.AtmId)
                .ToListAsync();
            return WithdrawalModelMapper.ToGetQuoteReturn(quote, cassettes, _timeProvider.GetUtcNow().UtcDateTime);
        }

        public async Task<WithdrawalModel.ConfirmQuote.Return> ConfirmQuote(WithdrawalModel.ConfirmQuote.Request request)
        {
            var idempotencyKey = ValidateIdempotencyKey(request.IdempotencyKey);

            await using var transaction = await _appDbContext.Database.BeginTransactionAsync();
            var atm = await _appDbContext.LoadAtmForUpdate(request.AtmId);
            var quote = await LoadQuote(request.AtmId, request.QuoteId);

            var isReplay = quote.Status == QuoteStatus.Confirmed && quote.ConfirmIdempotencyKey == idempotencyKey;
            if (isReplay)
            {
                return BuildReplayResponse(quote, atm, request.Option);
            }

            await EnsureKeyNotUsedByAnotherQuote(idempotencyKey, quote.Id);
            EnsureConfirmable(quote);

            var now = _timeProvider.GetUtcNow().UtcDateTime;
            if (quote.ExpiresAt <= now)
            {
                _quoteReservationReleaser.ReleaseReservation(atm, quote, QuoteStatus.Expired);
                await _appDbContext.SaveChangesAsync();
                await transaction.CommitAsync();
                throw new ConflictException(ErrorCodes.QUOTE_EXPIRED, "The quote has expired and its reservation was released.");
            }

            EnsureInService(atm, "confirm a withdrawal");

            var chosenOption = ChooseOption(quote, request.Option);
            DispenseNotes(atm, quote, chosenOption);

            quote.Status = QuoteStatus.Confirmed;
            quote.ConfirmedOptionId = chosenOption.Id;
            quote.ConfirmIdempotencyKey = idempotencyKey;
            quote.LimitAmount = chosenOption.Amount;
            quote.UpdatedAt = now;

            await _appDbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            return WithdrawalModelMapper.ToConfirmQuoteReturn(quote, chosenOption, atm.Cassettes);
        }

        public async Task<WithdrawalModel.CancelQuote.Return> CancelQuote(WithdrawalModel.CancelQuote.Request request)
        {
            await using var transaction = await _appDbContext.Database.BeginTransactionAsync();
            var atm = await _appDbContext.LoadAtmForUpdate(request.AtmId);
            var quote = await LoadQuote(request.AtmId, request.QuoteId);

            if (quote.Status == QuoteStatus.Confirmed)
            {
                throw new ConflictException(ErrorCodes.QUOTE_NOT_CONFIRMABLE, "A confirmed quote cannot be cancelled.");
            }

            var now = _timeProvider.GetUtcNow().UtcDateTime;
            if (quote.Status == QuoteStatus.Pending)
            {
                var newStatus = quote.ExpiresAt <= now ? QuoteStatus.Expired : QuoteStatus.Cancelled;
                _quoteReservationReleaser.ReleaseReservation(atm, quote, newStatus);
                await _appDbContext.SaveChangesAsync();
            }

            await transaction.CommitAsync();
            return WithdrawalModelMapper.ToCancelQuoteReturn(quote, atm.Cassettes, now);
        }

        private async Task<WithdrawalQuote> LoadQuote(Guid atmId, Guid quoteId)
        {
            var quote = await _appDbContext.WithdrawalQuotes
                .Include(withdrawalQuote => withdrawalQuote.Options)
                .ThenInclude(option => option.Items)
                .FirstOrDefaultAsync(withdrawalQuote => withdrawalQuote.Id == quoteId && withdrawalQuote.AtmId == atmId);
            return quote ?? throw QuoteNotFound(quoteId);
        }

        private static NotFoundException QuoteNotFound(Guid quoteId)
        {
            return new NotFoundException(ErrorCodes.QUOTE_NOT_FOUND, $"Quote not found: {quoteId}");
        }

        private static string ValidateIdempotencyKey(string? idempotencyKey)
        {
            if (string.IsNullOrWhiteSpace(idempotencyKey))
            {
                throw new RequestValidationException(ErrorCodes.IDEMPOTENCY_KEY_REQUIRED,
                    $"The {WithdrawalRules.IDEMPOTENCY_KEY_HEADER_NAME} header is required to confirm a quote.");
            }

            var trimmedKey = idempotencyKey.Trim();
            if (trimmedKey.Length > WithdrawalRules.IDEMPOTENCY_KEY_MAX_LENGTH)
            {
                throw new RequestValidationException(ErrorCodes.IDEMPOTENCY_KEY_REQUIRED,
                    $"The {WithdrawalRules.IDEMPOTENCY_KEY_HEADER_NAME} header must be at most {WithdrawalRules.IDEMPOTENCY_KEY_MAX_LENGTH} characters.");
            }

            return trimmedKey;
        }

        private async Task EnsureKeyNotUsedByAnotherQuote(string idempotencyKey, Guid quoteId)
        {
            var keyUsedElsewhere = await _appDbContext.WithdrawalQuotes.AnyAsync(withdrawalQuote =>
                withdrawalQuote.ConfirmIdempotencyKey == idempotencyKey && withdrawalQuote.Id != quoteId);
            if (keyUsedElsewhere)
            {
                throw new ConflictException(ErrorCodes.IDEMPOTENCY_KEY_REUSED,
                    "This idempotency key was already used to confirm a different quote.");
            }
        }

        private static void EnsureConfirmable(WithdrawalQuote quote)
        {
            if (quote.Status == QuoteStatus.Pending) return;

            throw new ConflictException(ErrorCodes.QUOTE_NOT_CONFIRMABLE, $"A {quote.Status} quote cannot be confirmed.");
        }

        private static void EnsureInService(AtmMachine atm, string action)
        {
            if (atm.Status == AtmStatus.InService) return;

            throw new ConflictException(ErrorCodes.INVALID_ATM_STATE, $"Cannot {action} while the ATM is {atm.Status}; it must be {AtmStatus.InService}.");
        }

        private WithdrawalModel.ConfirmQuote.Return BuildReplayResponse(WithdrawalQuote quote, AtmMachine atm, QuoteOptionType? requestedOption)
        {
            var confirmedOption = quote.Options.FirstOrDefault(option => option.Id == quote.ConfirmedOptionId);
            if (confirmedOption == null)
            {
                throw new InvalidOperationException($"Confirmed quote {quote.Id} has no confirmed option.");
            }

            var isDifferentRequest = requestedOption.HasValue && requestedOption.Value != confirmedOption.OptionType;
            if (isDifferentRequest)
            {
                throw new ConflictException(ErrorCodes.IDEMPOTENCY_KEY_REUSED,
                    "This idempotency key was already used with a different option for the same quote.");
            }

            return WithdrawalModelMapper.ToConfirmQuoteReturn(quote, confirmedOption, atm.Cassettes);
        }

        private static WithdrawalQuoteOption ChooseOption(WithdrawalQuote quote, QuoteOptionType? requestedOption)
        {
            if (requestedOption.HasValue)
            {
                var requested = quote.Options.FirstOrDefault(option => option.OptionType == requestedOption.Value);
                return requested ?? throw new BusinessRuleException(ErrorCodes.QUOTE_OPTION_NOT_OFFERED,
                    $"The quote does not offer the {requestedOption.Value} option.");
            }

            if (quote.Options.Count != 1)
            {
                throw new BusinessRuleException(ErrorCodes.QUOTE_OPTION_REQUIRED,
                    "This quote offers several alternatives; choose one with the option field.");
            }

            return quote.Options.Single();
        }

        private async Task<decimal> GetRemainingDailyLimit(string cardHash, Currency currency, DateTime now)
        {
            var dayStart = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc);
            var usedAmount = await _appDbContext.WithdrawalQuotes
                .Where(quote => quote.CardHash == cardHash
                    && quote.Currency == currency
                    && quote.CreatedAt >= dayStart
                    && (quote.Status == QuoteStatus.Confirmed || (quote.Status == QuoteStatus.Pending && quote.ExpiresAt > now)))
                .SumAsync(quote => (decimal?)quote.LimitAmount) ?? 0m;

            return WithdrawalRules.DailyLimits[currency] - usedAmount;
        }

        private static List<NoteStock> BuildNoteStocks(AtmMachine atm, Currency currency)
        {
            return atm.Cassettes
                .Where(cassette => cassette.Kind == CassetteKind.Recycling && cassette.Currency == currency)
                .SelectMany(cassette => cassette.Contents.Select(content =>
                    new NoteStock(cassette.Id, cassette.Position, content.Denomination, content.AvailableCount, cassette.Capacity)))
                .ToList();
        }

        private static List<(QuoteOptionType OptionType, NoteBreakdown Breakdown)> GetOfferedBreakdowns(NoteSelectionResult selection, decimal remainingLimit)
        {
            var offered = new List<(QuoteOptionType, NoteBreakdown)>();
            if (selection.Exact != null) offered.Add((QuoteOptionType.Exact, selection.Exact));
            if (selection.Lower != null) offered.Add((QuoteOptionType.Lower, selection.Lower));
            if (selection.Upper != null && selection.Upper.Amount <= remainingLimit) offered.Add((QuoteOptionType.Upper, selection.Upper));
            return offered;
        }

        private WithdrawalQuote BuildQuote(
            AtmMachine atm,
            WithdrawalModel.CreateQuote.Request request,
            string cardHash,
            string maskedCardNumber,
            List<(QuoteOptionType OptionType, NoteBreakdown Breakdown)> offeredBreakdowns,
            DateTime now)
        {
            var quote = new WithdrawalQuote
            {
                Id = Guid.NewGuid(),
                AtmId = atm.Id,
                CardHash = cardHash,
                MaskedCardNumber = maskedCardNumber,
                Currency = request.Currency,
                RequestedAmount = request.Amount,
                LimitAmount = offeredBreakdowns.Max(offered => offered.Breakdown.Amount),
                Status = QuoteStatus.Pending,
                CreatedAt = now,
                ExpiresAt = now + WithdrawalRules.QuoteValidity
            };

            foreach (var (optionType, breakdown) in offeredBreakdowns)
            {
                quote.Options.Add(BuildOption(quote, optionType, breakdown));
            }

            return quote;
        }

        private static WithdrawalQuoteOption BuildOption(WithdrawalQuote quote, QuoteOptionType optionType, NoteBreakdown breakdown)
        {
            var option = new WithdrawalQuoteOption
            {
                Id = Guid.NewGuid(),
                QuoteId = quote.Id,
                OptionType = optionType,
                Amount = breakdown.Amount
            };

            foreach (var allocation in breakdown.Allocations)
            {
                option.Items.Add(new WithdrawalQuoteOptionItem
                {
                    Id = Guid.NewGuid(),
                    OptionId = option.Id,
                    CassetteId = allocation.CassetteId,
                    Denomination = allocation.Denomination,
                    NoteCount = allocation.NoteCount
                });
            }

            return option;
        }

        private void ReserveNotes(AtmMachine atm, WithdrawalQuote quote)
        {
            foreach (var ((cassetteId, denomination), reservedCount) in QuoteReservationCalculator.GetReservedCounts(quote))
            {
                var cassette = FindCassette(atm, cassetteId);
                var content = _cassetteMovementRecorder.GetOrAddContent(cassette, quote.Currency, denomination);
                _cassetteMovementRecorder.Record(new CassetteMovementRequest
                {
                    Cassette = cassette,
                    Content = content,
                    MovementType = CassetteMovementType.Reservation,
                    ReservedCountDelta = reservedCount,
                    ReferenceId = quote.Id
                });
            }
        }

        private void DispenseNotes(AtmMachine atm, WithdrawalQuote quote, WithdrawalQuoteOption chosenOption)
        {
            var chosenCounts = QuoteReservationCalculator.GetOptionCounts(chosenOption);

            foreach (var ((cassetteId, denomination), reservedCount) in QuoteReservationCalculator.GetReservedCounts(quote))
            {
                var cassette = FindCassette(atm, cassetteId);
                var content = _cassetteMovementRecorder.GetOrAddContent(cassette, quote.Currency, denomination);
                var dispensedCount = chosenCounts.GetValueOrDefault((cassetteId, denomination));
                var unusedCount = reservedCount - dispensedCount;

                if (dispensedCount > 0)
                {
                    _cassetteMovementRecorder.Record(new CassetteMovementRequest
                    {
                        Cassette = cassette,
                        Content = content,
                        MovementType = CassetteMovementType.Withdrawal,
                        NoteCountDelta = -dispensedCount,
                        ReservedCountDelta = -dispensedCount,
                        ReferenceId = quote.Id
                    });
                }

                if (unusedCount > 0)
                {
                    _cassetteMovementRecorder.Record(new CassetteMovementRequest
                    {
                        Cassette = cassette,
                        Content = content,
                        MovementType = CassetteMovementType.ReservationRelease,
                        ReservedCountDelta = -unusedCount,
                        ReferenceId = quote.Id
                    });
                }
            }
        }

        private static Cassette FindCassette(AtmMachine atm, Guid cassetteId)
        {
            var cassette = atm.Cassettes.FirstOrDefault(atmCassette => atmCassette.Id == cassetteId);
            return cassette ?? throw new InvalidOperationException($"Cassette {cassetteId} does not belong to ATM {atm.Id}.");
        }
    }
}
