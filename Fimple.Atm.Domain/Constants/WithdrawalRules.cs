using Fimple.Atm.Domain.Enums;

namespace Fimple.Atm.Domain.Constants
{
    public static class WithdrawalRules
    {
        public static readonly TimeSpan QuoteValidity = TimeSpan.FromSeconds(60);

        public static readonly IReadOnlyDictionary<Currency, decimal> DailyLimits = new Dictionary<Currency, decimal>
        {
            [Currency.Try] = 20000m,
            [Currency.Usd] = 1000m,
            [Currency.Eur] = 1000m
        };
    }
}
