using Fimple.Atm.Domain.Entities;
using Fimple.Atm.Domain.Enums;

namespace Fimple.Atm.Domain.Interfaces
{
    public interface IWithdrawalQuoteRepository
    {
        public Task<WithdrawalQuote?> Find(Guid atmId, Guid quoteId);
        public Task<WithdrawalQuote?> FindReadOnly(Guid atmId, Guid quoteId);
        public Task<List<WithdrawalQuote>> GetExpiredPending(Guid atmId, DateTime now);
        public Task<List<WithdrawalQuote>> GetPending(Guid atmId);
        public Task<List<Guid>> GetAtmIdsWithExpiredPending(DateTime now);
        public Task<bool> IsIdempotencyKeyUsedByAnotherQuote(string idempotencyKey, Guid quoteId);
        public Task<decimal> GetUsedLimitAmount(string cardHash, Currency currency, DateTime dayStart, DateTime now);
        public Task LockCard(string cardHash);
        public void Add(WithdrawalQuote quote);
    }
}
