using Fimple.Atm.Domain.Enums;

namespace Fimple.Atm.Domain.Entities
{
    public class Cassette
    {
        public Guid Id { get; set; }
        public Guid AtmId { get; set; }
        public Atm Atm { get; set; } = null!;
        public int Position { get; set; }
        public CassetteKind Kind { get; set; }
        public Currency? Currency { get; set; }
        public Denomination? Denomination { get; set; }
        public int Capacity { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public ICollection<CassetteContent> Contents { get; set; } = new List<CassetteContent>();
    }
}
