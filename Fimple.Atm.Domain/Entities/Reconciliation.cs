using Fimple.Atm.Domain.Enums;

namespace Fimple.Atm.Domain.Entities
{
    public class Reconciliation
    {
        public Guid Id { get; set; }
        public Guid AtmId { get; set; }
        public AtmMachine Atm { get; set; } = null!;
        public ReconciliationStatus Status { get; set; }
        public decimal TotalDifferenceInBaseCurrency { get; set; }
        public required string CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public ICollection<ReconciliationLine> Lines { get; set; } = new List<ReconciliationLine>();
    }
}
