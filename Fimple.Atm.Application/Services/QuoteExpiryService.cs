using Fimple.Atm.Domain.Interfaces;

namespace Fimple.Atm.Application.Services
{
    public class QuoteExpiryService : IQuoteExpiryService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAtmRepository _atmRepository;
        private readonly IWithdrawalQuoteRepository _quoteRepository;
        private readonly IQuoteReservationReleaser _quoteReservationReleaser;
        private readonly TimeProvider _timeProvider;

        public QuoteExpiryService(
            IUnitOfWork unitOfWork,
            IAtmRepository atmRepository,
            IWithdrawalQuoteRepository quoteRepository,
            IQuoteReservationReleaser quoteReservationReleaser,
            TimeProvider timeProvider)
        {
            _unitOfWork = unitOfWork;
            _atmRepository = atmRepository;
            _quoteRepository = quoteRepository;
            _quoteReservationReleaser = quoteReservationReleaser;
            _timeProvider = timeProvider;
        }

        public async Task<int> ReleaseExpiredQuotes()
        {
            var now = _timeProvider.GetUtcNow().UtcDateTime;
            var atmIds = await _quoteRepository.GetAtmIdsWithExpiredPending(now);

            var releasedCount = 0;
            foreach (var atmId in atmIds)
            {
                releasedCount += await ReleaseExpiredQuotesOfAtm(atmId);
            }

            return releasedCount;
        }

        private async Task<int> ReleaseExpiredQuotesOfAtm(Guid atmId)
        {
            await using var transaction = await _unitOfWork.BeginTransaction();
            var atm = await _atmRepository.LoadForUpdate(atmId);
            var releasedCount = await _quoteReservationReleaser.ReleaseExpiredQuotes(atm);
            await _unitOfWork.SaveChanges();
            await transaction.Commit();

            _unitOfWork.ClearTracking();
            return releasedCount;
        }
    }
}
