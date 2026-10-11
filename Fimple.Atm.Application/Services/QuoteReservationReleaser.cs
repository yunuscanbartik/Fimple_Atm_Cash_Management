using Fimple.Atm.Domain.Entities;
using Fimple.Atm.Domain.Enums;
using Fimple.Atm.Domain.Interfaces;

namespace Fimple.Atm.Application.Services
{
    public class QuoteReservationReleaser : IQuoteReservationReleaser
    {
        private readonly IWithdrawalQuoteRepository _quoteRepository;
        private readonly ICassetteMovementRecorder _cassetteMovementRecorder;
        private readonly TimeProvider _timeProvider;

        public QuoteReservationReleaser(IWithdrawalQuoteRepository quoteRepository, ICassetteMovementRecorder cassetteMovementRecorder, TimeProvider timeProvider)
        {
            _quoteRepository = quoteRepository;
            _cassetteMovementRecorder = cassetteMovementRecorder;
            _timeProvider = timeProvider;
        }

        public void ReleaseReservation(AtmMachine atm, WithdrawalQuote quote, QuoteStatus newStatus)
        {
            var reservedCounts = QuoteReservationCalculator.GetReservedCounts(quote);
            foreach (var ((cassetteId, denomination), reservedCount) in reservedCounts)
            {
                var cassette = atm.Cassettes.FirstOrDefault(atmCassette => atmCassette.Id == cassetteId);
                if (cassette == null)
                {
                    throw new InvalidOperationException($"Quote {quote.Id} references cassette {cassetteId}, which does not belong to ATM {atm.Id}.");
                }

                var content = _cassetteMovementRecorder.GetOrAddContent(cassette, quote.Currency, denomination);
                _cassetteMovementRecorder.Record(new CassetteMovementRequest
                {
                    Cassette = cassette,
                    Content = content,
                    MovementType = CassetteMovementType.ReservationRelease,
                    ReservedCountDelta = -reservedCount,
                    ReferenceId = quote.Id
                });
            }

            quote.Status = newStatus;
            quote.LimitAmount = 0m;
            quote.UpdatedAt = _timeProvider.GetUtcNow().UtcDateTime;
        }

        public async Task<int> ReleaseExpiredQuotes(AtmMachine atm)
        {
            var now = _timeProvider.GetUtcNow().UtcDateTime;
            var expiredQuotes = await _quoteRepository.GetExpiredPending(atm.Id, now);

            foreach (var expiredQuote in expiredQuotes)
            {
                ReleaseReservation(atm, expiredQuote, QuoteStatus.Expired);
            }

            return expiredQuotes.Count;
        }

        public async Task<int> ReleasePendingQuotes(AtmMachine atm, QuoteStatus newStatus)
        {
            var pendingQuotes = await _quoteRepository.GetPending(atm.Id);

            foreach (var pendingQuote in pendingQuotes)
            {
                ReleaseReservation(atm, pendingQuote, newStatus);
            }

            return pendingQuotes.Count;
        }
    }
}
