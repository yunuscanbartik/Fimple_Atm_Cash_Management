using Fimple.Atm.Domain.Enums;

namespace Fimple.Atm.Domain.Constants
{
    public static class AtmStatusRules
    {
        private static readonly AtmStatus[] CassetteConfigurationStatuses =
        {
            AtmStatus.Unconfigured, AtmStatus.Configured, AtmStatus.Maintenance
        };

        private static readonly AtmStatus[] CashLoadingStatuses =
        {
            AtmStatus.Configured, AtmStatus.Loaded, AtmStatus.Maintenance
        };

        private static readonly AtmStatus[] CashUnloadingStatuses =
        {
            AtmStatus.Loaded, AtmStatus.Maintenance
        };

        private static readonly AtmStatus[] ServiceEntryStatuses =
        {
            AtmStatus.Loaded, AtmStatus.Maintenance
        };

        public static bool CanConfigureCassettes(AtmStatus status) => CassetteConfigurationStatuses.Contains(status);

        public static bool CanLoadCash(AtmStatus status) => CashLoadingStatuses.Contains(status);

        public static bool CanUnloadCash(AtmStatus status) => CashUnloadingStatuses.Contains(status);

        public static bool CanPutInService(AtmStatus status) => ServiceEntryStatuses.Contains(status);

        public static bool CanEnterMaintenance(AtmStatus status) => status == AtmStatus.InService;
    }
}
