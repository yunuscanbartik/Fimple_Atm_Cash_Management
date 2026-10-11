using Fimple.Atm.Domain.Enums;

namespace Fimple.Atm.Domain.Entities
{
    public class Atm
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public AtmStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public ICollection<Cassette> Cassettes { get; set; } = new List<Cassette>();
    }
}
