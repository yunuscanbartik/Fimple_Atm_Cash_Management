using Fimple.Atm.Domain.Enums;

namespace Fimple.Atm.Domain.Entities
{
    public class CassetteMovement
    {
        public Guid Id { get; set; }
        public Guid CassetteId { get; set; }
        public Cassette Cassette { get; set; } = null!;
        public CassetteMovementType MovementType { get; set; }
        public Currency Currency { get; set; }
        public Denomination Denomination { get; set; }
        public int NoteCountDelta { get; set; }
        public int ReservedCountDelta { get; set; }
        public Guid? ReferenceId { get; set; }
        public string? PerformedBy { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
