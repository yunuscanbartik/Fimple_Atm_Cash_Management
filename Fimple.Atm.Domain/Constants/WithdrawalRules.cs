using Fimple.Atm.Domain.Enums;

namespace Fimple.Atm.Domain.Constants
{
    public static class WithdrawalRules
    {
        public const string CARD_NUMBER_PATTERN = @"^\d{12,19}$";
        public const int MASKED_CARD_VISIBLE_DIGIT_COUNT = 4;
        public const int IDEMPOTENCY_KEY_MAX_LENGTH = 128;
        public const string IDEMPOTENCY_KEY_HEADER_NAME = "Idempotency-Key";

        public static readonly TimeSpan QuoteValidity = TimeSpan.FromSeconds(60);

        public static readonly IReadOnlyDictionary<Currency, decimal> DailyLimits = new Dictionary<Currency, decimal>
        {
            [Currency.Try] = 20000m,
            [Currency.Usd] = 1000m,
            [Currency.Eur] = 1000m
        };
    }
}
