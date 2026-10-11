using Fimple.Atm.Domain.Enums;

namespace Fimple.Atm.Domain.Entities
{
    public class ReconciliationLine
    {
        public Guid Id { get; set; }
        public Guid ReconciliationId { get; set; }
        public Reconciliation Reconciliation { get; set; } = null!;
        public Guid CassetteId { get; set; }
        public Cassette Cassette { get; set; } = null!;
        public Currency Currency { get; set; }
        public Denomination Denomination { get; set; }
        public int SystemCount { get; set; }
        public int CountedCount { get; set; }
        public int Difference { get; set; }
    }
}
