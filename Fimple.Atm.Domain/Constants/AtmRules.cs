namespace Fimple.Atm.Domain.Constants
{
    public static class AtmRules
    {
        public const int CassetteCount = 5;
        public const int CassetteCapacity = 100;
        public const int MinForeignCurrencyRecyclingCassetteCount = 1;
        public const int MaxForeignCurrencyRecyclingCassetteCount = 2;
        public const decimal LowCashThresholdRatio = 0.10m;
    }
}
