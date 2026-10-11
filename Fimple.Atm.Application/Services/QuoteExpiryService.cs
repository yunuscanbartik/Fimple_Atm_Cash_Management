using Fimple.Atm.Domain.Enums;
using Fimple.Atm.Domain.Interfaces;
using Fimple.Atm.Infrastructure.PostgreSQL;
using Microsoft.EntityFrameworkCore;

namespace Fimple.Atm.Application.Services
{
    public class QuoteExpiryService : IQuoteExpiryService
    {
        private readonly AppDbContext _appDbContext;
        private readonly IQuoteReservationReleaser _quoteReservationReleaser;
        private readonly TimeProvider _timeProvider;

        public QuoteExpiryService(AppDbContext appDbContext, IQuoteReservationReleaser quoteReservationReleaser, TimeProvider timeProvider)
        {
            _appDbContext = appDbContext;
            _quoteReservationReleaser = quoteReservationReleaser;
            _timeProvider = timeProvider;
        }

        public async Task<int> ReleaseExpiredQuotes()
        {
            var now = _timeProvider.GetUtcNow().UtcDateTime;
            var atmIds = await _appDbContext.WithdrawalQuotes
                .AsNoTracking()
                .Where(quote => quote.Status == QuoteStatus.Pending && quote.ExpiresAt <= now)
                .Select(quote => quote.AtmId)
                .Distinct()
                .ToListAsync();

            var releasedCount = 0;
            foreach (var atmId in atmIds)
            {
                releasedCount += await ReleaseExpiredQuotesOfAtm(atmId);
            }

            return releasedCount;
        }

        private async Task<int> ReleaseExpiredQuotesOfAtm(Guid atmId)
        {
            await using var transaction = await _appDbContext.Database.BeginTransactionAsync();
            var atm = await _appDbContext.LoadAtmForUpdate(atmId);
            var releasedCount = await _quoteReservationReleaser.ReleaseExpiredQuotes(atm);
            await _appDbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            _appDbContext.ChangeTracker.Clear();
            return releasedCount;
        }
    }
}
