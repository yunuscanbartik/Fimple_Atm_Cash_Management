namespace Fimple.Atm.Domain.Constants
{
    public static class AtmRules
    {
        public const int CASSETTE_COUNT = 5;
        public const int CASSETTE_CAPACITY = 100;
        public const int MIN_FOREIGN_CURRENCY_RECYCLING_CASSETTE_COUNT = 1;
        public const int MAX_FOREIGN_CURRENCY_RECYCLING_CASSETTE_COUNT = 2;
        public const decimal LOW_CASH_THRESHOLD_RATIO = 0.10m;
    }
}
