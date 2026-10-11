using Fimple.Atm.Domain.Enums;

namespace Fimple.Atm.Domain.Entities
{
    public class CassetteContent
    {
        public Guid Id { get; set; }
        public Guid CassetteId { get; set; }
        public Cassette Cassette { get; set; } = null!;
        public Currency Currency { get; set; }
        public Denomination Denomination { get; set; }
        public int NoteCount { get; set; }
        public int ReservedCount { get; set; }
        public int AvailableCount => NoteCount - ReservedCount;
    }
}
