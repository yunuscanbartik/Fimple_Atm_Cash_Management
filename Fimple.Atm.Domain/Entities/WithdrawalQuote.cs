using Fimple.Atm.Domain.Enums;

namespace Fimple.Atm.Domain.Entities
{
    public class WithdrawalQuote
    {
        public Guid Id { get; set; }
        public Guid AtmId { get; set; }
        public AtmMachine Atm { get; set; } = null!;
        public required string CardHash { get; set; }
        public required string MaskedCardNumber { get; set; }
        public Currency Currency { get; set; }
        public decimal RequestedAmount { get; set; }
        public QuoteStatus Status { get; set; }
        public Guid? ConfirmedOptionId { get; set; }
        public string? ConfirmIdempotencyKey { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public ICollection<WithdrawalQuoteOption> Options { get; set; } = new List<WithdrawalQuoteOption>();
    }
}
