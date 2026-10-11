using Fimple.Atm.Domain.Enums;

namespace Fimple.Atm.Domain.Models
{
    public class LowCashAlertPayload
    {
        public required Guid AtmId { get; set; }
        public required Guid CassetteId { get; set; }
        public required int CassettePosition { get; set; }
        public required Currency Currency { get; set; }
        public required int Denomination { get; set; }
        public required int AvailableCount { get; set; }
        public required int Capacity { get; set; }
        public required CassetteMovementType TriggeredBy { get; set; }
        public Guid? ReferenceId { get; set; }
    }
}
