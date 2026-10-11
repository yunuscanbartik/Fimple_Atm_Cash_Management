using Fimple.Atm.Domain.Enums;

namespace Fimple.Atm.Domain.Constants
{
    public static class ReconciliationRules
    {
        public const decimal APPROVAL_THRESHOLD_IN_BASE_CURRENCY = 1000m;
        public const Currency BASE_CURRENCY = Currency.Try;
    }
}
