using Fimple.Atm.Domain.Enums;

namespace Fimple.Atm.Domain.Constants
{
    public static class WithdrawalRules
    {
        public const string CardNumberPattern = @"^\d{12,19}$";
        public const int MaskedCardVisibleDigitCount = 4;
        public const int IdempotencyKeyMaxLength = 128;
        public const string IdempotencyKeyHeaderName = "Idempotency-Key";

        public static readonly TimeSpan QuoteValidity = TimeSpan.FromSeconds(60);

        public static readonly IReadOnlyDictionary<Currency, decimal> DailyLimits = new Dictionary<Currency, decimal>
        {
            [Currency.Try] = 20000m,
            [Currency.Usd] = 1000m,
            [Currency.Eur] = 1000m
        };
    }
}
