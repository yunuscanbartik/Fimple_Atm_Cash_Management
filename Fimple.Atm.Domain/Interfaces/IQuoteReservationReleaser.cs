using Fimple.Atm.Domain.Entities;
using Fimple.Atm.Domain.Enums;

namespace Fimple.Atm.Domain.Interfaces
{
    public interface IQuoteReservationReleaser
    {
        public void ReleaseReservation(AtmMachine atm, WithdrawalQuote quote, QuoteStatus newStatus);
        public Task<int> ReleaseExpiredQuotes(AtmMachine atm);
        public Task<int> ReleasePendingQuotes(AtmMachine atm, QuoteStatus newStatus);
    }
}
