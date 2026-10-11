using Fimple.Atm.Domain.Enums;

namespace Fimple.Atm.Domain.Entities
{
    public class ExchangeRate
    {
        public Guid Id { get; set; }
        public Currency Currency { get; set; }
        public decimal RateToBaseCurrency { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
