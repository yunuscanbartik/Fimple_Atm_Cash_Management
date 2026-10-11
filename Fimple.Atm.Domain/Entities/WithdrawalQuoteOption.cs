using Fimple.Atm.Domain.Enums;

namespace Fimple.Atm.Domain.Entities
{
    public class WithdrawalQuoteOption
    {
        public Guid Id { get; set; }
        public Guid QuoteId { get; set; }
        public WithdrawalQuote Quote { get; set; } = null!;
        public QuoteOptionType OptionType { get; set; }
        public decimal Amount { get; set; }
        public ICollection<WithdrawalQuoteOptionItem> Items { get; set; } = new List<WithdrawalQuoteOptionItem>();
    }
}
