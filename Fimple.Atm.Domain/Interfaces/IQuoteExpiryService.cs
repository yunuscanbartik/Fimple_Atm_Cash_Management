namespace Fimple.Atm.Domain.Interfaces
{
    public interface IQuoteExpiryService
    {
        public Task<int> ReleaseExpiredQuotes();
    }
}
