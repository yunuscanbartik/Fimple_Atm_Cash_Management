namespace Fimple.Atm.Domain.Entities
{
    public class DepositTransaction
    {
        public Guid Id { get; set; }
        public Guid AtmId { get; set; }
        public AtmMachine Atm { get; set; } = null!;
        public int UnrecognisedNoteCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public ICollection<DepositNote> Notes { get; set; } = new List<DepositNote>();
    }
}
