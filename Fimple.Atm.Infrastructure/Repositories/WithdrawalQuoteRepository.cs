using Fimple.Atm.Domain.Entities;
using Fimple.Atm.Domain.Enums;
using Fimple.Atm.Domain.Interfaces;
using Fimple.Atm.Infrastructure.PostgreSQL;
using Microsoft.EntityFrameworkCore;

namespace Fimple.Atm.Infrastructure.Repositories
{
    public class WithdrawalQuoteRepository : IWithdrawalQuoteRepository
    {
        private readonly AppDbContext _appDbContext;

        public WithdrawalQuoteRepository(AppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }

        public async Task<WithdrawalQuote?> Find(Guid atmId, Guid quoteId)
        {
            return await _appDbContext.WithdrawalQuotes
                .Include(quote => quote.Options)
                .ThenInclude(option => option.Items)
                .FirstOrDefaultAsync(quote => quote.Id == quoteId && quote.AtmId == atmId);
        }

        public async Task<WithdrawalQuote?> FindReadOnly(Guid atmId, Guid quoteId)
        {
            return await _appDbContext.WithdrawalQuotes
                .AsNoTracking()
                .Include(quote => quote.Options)
                .ThenInclude(option => option.Items)
                .FirstOrDefaultAsync(quote => quote.Id == quoteId && quote.AtmId == atmId);
        }

        public async Task<List<WithdrawalQuote>> GetExpiredPending(Guid atmId, DateTime now)
        {
            return await _appDbContext.WithdrawalQuotes
                .Include(quote => quote.Options)
                .ThenInclude(option => option.Items)
                .Where(quote => quote.AtmId == atmId && quote.Status == QuoteStatus.Pending && quote.ExpiresAt <= now)
                .ToListAsync();
        }

        public async Task<List<WithdrawalQuote>> GetPending(Guid atmId)
        {
            return await _appDbContext.WithdrawalQuotes
                .Include(quote => quote.Options)
                .ThenInclude(option => option.Items)
                .Where(quote => quote.AtmId == atmId && quote.Status == QuoteStatus.Pending)
                .ToListAsync();
        }

        public async Task<List<Guid>> GetAtmIdsWithExpiredPending(DateTime now)
        {
            return await _appDbContext.WithdrawalQuotes
                .AsNoTracking()
                .Where(quote => quote.Status == QuoteStatus.Pending && quote.ExpiresAt <= now)
                .Select(quote => quote.AtmId)
                .Distinct()
                .ToListAsync();
        }

        public async Task<bool> IsIdempotencyKeyUsedByAnotherQuote(string idempotencyKey, Guid quoteId)
        {
            return await _appDbContext.WithdrawalQuotes.AnyAsync(quote =>
                quote.ConfirmIdempotencyKey == idempotencyKey && quote.Id != quoteId);
        }

        public async Task<decimal> GetUsedLimitAmount(string cardHash, Currency currency, DateTime dayStart, DateTime now)
        {
            var usedAmount = await _appDbContext.WithdrawalQuotes
                .Where(quote => quote.CardHash == cardHash
                    && quote.Currency == currency
                    && quote.CreatedAt >= dayStart
                    && (quote.Status == QuoteStatus.Confirmed || (quote.Status == QuoteStatus.Pending && quote.ExpiresAt > now)))
                .SumAsync(quote => (decimal?)quote.LimitAmount);
            return usedAmount ?? 0m;
        }

        public async Task LockCard(string cardHash)
        {
            await _appDbContext.LockCardForUpdate(cardHash);
        }

        public void Add(WithdrawalQuote quote)
        {
            _appDbContext.WithdrawalQuotes.Add(quote);
        }
    }
}
