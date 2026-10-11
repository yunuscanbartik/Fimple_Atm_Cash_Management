using Fimple.Atm.Domain.Enums;

namespace Fimple.Atm.Domain.Entities
{
    public class WithdrawalQuoteOptionItem
    {
        public Guid Id { get; set; }
        public Guid OptionId { get; set; }
        public WithdrawalQuoteOption Option { get; set; } = null!;
        public Guid CassetteId { get; set; }
        public Cassette Cassette { get; set; } = null!;
        public Denomination Denomination { get; set; }
        public int NoteCount { get; set; }
    }
}
