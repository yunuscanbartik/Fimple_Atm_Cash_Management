using Fimple.Atm.Domain.Enums;

namespace Fimple.Atm.Domain.Constants
{
    public static class ReconciliationRules
    {
        public const decimal ApprovalThresholdInBaseCurrency = 1000m;
        public const Currency BaseCurrency = Currency.Try;
    }
}
