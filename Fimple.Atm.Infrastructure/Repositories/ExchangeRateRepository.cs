using Fimple.Atm.Domain.Enums;
using Fimple.Atm.Domain.Interfaces;
using Fimple.Atm.Infrastructure.PostgreSQL;
using Microsoft.EntityFrameworkCore;

namespace Fimple.Atm.Infrastructure.Repositories
{
    public class ExchangeRateRepository : IExchangeRateRepository
    {
        private readonly AppDbContext _appDbContext;

        public ExchangeRateRepository(AppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }

        public async Task<Dictionary<Currency, decimal>> GetRatesToBaseCurrency()
        {
            return await _appDbContext.ExchangeRates
                .AsNoTracking()
                .ToDictionaryAsync(exchangeRate => exchangeRate.Currency, exchangeRate => exchangeRate.RateToBaseCurrency);
        }
    }
}
