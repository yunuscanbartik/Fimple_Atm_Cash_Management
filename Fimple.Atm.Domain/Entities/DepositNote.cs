using Fimple.Atm.Domain.Enums;

namespace Fimple.Atm.Domain.Entities
{
    public class DepositNote
    {
        public Guid Id { get; set; }
        public Guid DepositTransactionId { get; set; }
        public DepositTransaction DepositTransaction { get; set; } = null!;
        public Currency Currency { get; set; }
        public Denomination Denomination { get; set; }
        public int NoteCount { get; set; }
        public Guid? CassetteId { get; set; }
        public Cassette? Cassette { get; set; }
    }
}
