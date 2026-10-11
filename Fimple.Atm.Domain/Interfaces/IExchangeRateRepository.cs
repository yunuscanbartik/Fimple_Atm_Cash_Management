using Fimple.Atm.Domain.Enums;

namespace Fimple.Atm.Domain.Interfaces
{
    public interface IExchangeRateRepository
    {
        public Task<Dictionary<Currency, decimal>> GetRatesToBaseCurrency();
    }
}
