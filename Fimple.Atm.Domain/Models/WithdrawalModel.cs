using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Fimple.Atm.Domain.Constants;
using Fimple.Atm.Domain.Enums;

namespace Fimple.Atm.Domain.Models
{
    public class WithdrawalModel
    {
        public class NoteItem
        {
            public required Guid CassetteId { get; set; }
            public required int CassettePosition { get; set; }
            public required int Denomination { get; set; }
            public required int NoteCount { get; set; }
        }

        public class OptionItem
        {
            public required QuoteOptionType OptionType { get; set; }
            public required decimal Amount { get; set; }
            public required int NoteCount { get; set; }
            public required List<NoteItem> Notes { get; set; }
        }

        public class CreateQuote
        {
            public class Request : AtmRequestBase
            {
                [RegularExpression(WithdrawalRules.CARD_NUMBER_PATTERN, ErrorMessage = "Card number must be 12 to 19 digits")]
                public required string CardNumber { get; set; }

                public required Currency Currency { get; set; }

                [Range(1, int.MaxValue, ErrorMessage = "Amount must be a positive whole number")]
                public required int Amount { get; set; }
            }

            public class Return
            {
                public required Guid QuoteId { get; set; }
                public required Guid AtmId { get; set; }
                public required Currency Currency { get; set; }
                public required decimal RequestedAmount { get; set; }
                public required QuoteStatus Status { get; set; }
                public required string MaskedCardNumber { get; set; }
                public required DateTime ExpiresAt { get; set; }
                public required bool IsExactMatch { get; set; }
                public required List<OptionItem> Options { get; set; }
            }
        }

        public class GetQuote
        {
            public class Request : AtmRequestBase
            {
                [JsonIgnore]
                public Guid QuoteId { get; set; }
            }

            public class Return
            {
                public required Guid QuoteId { get; set; }
                public required Guid AtmId { get; set; }
                public required Currency Currency { get; set; }
                public required decimal RequestedAmount { get; set; }
                public required QuoteStatus Status { get; set; }
                public required string MaskedCardNumber { get; set; }
                public required DateTime ExpiresAt { get; set; }
                public required bool IsExactMatch { get; set; }
                public required List<OptionItem> Options { get; set; }
            }
        }

        public class ConfirmQuote
        {
            public class Request : AtmRequestBase
            {
                [JsonIgnore]
                public Guid QuoteId { get; set; }

                [JsonIgnore]
                public string? IdempotencyKey { get; set; }

                public QuoteOptionType? Option { get; set; }
            }

            public class Return
            {
                public required Guid QuoteId { get; set; }
                public required Guid AtmId { get; set; }
                public required QuoteStatus Status { get; set; }
                public required Currency Currency { get; set; }
                public required QuoteOptionType OptionType { get; set; }
                public required decimal Amount { get; set; }
                public required int NoteCount { get; set; }
                public required List<NoteItem> Notes { get; set; }
            }
        }

        public class CancelQuote
        {
            public class Request : AtmRequestBase
            {
                [JsonIgnore]
                public Guid QuoteId { get; set; }
            }

            public class Return
            {
                public required Guid QuoteId { get; set; }
                public required Guid AtmId { get; set; }
                public required Currency Currency { get; set; }
                public required decimal RequestedAmount { get; set; }
                public required QuoteStatus Status { get; set; }
                public required string MaskedCardNumber { get; set; }
                public required DateTime ExpiresAt { get; set; }
                public required bool IsExactMatch { get; set; }
                public required List<OptionItem> Options { get; set; }
            }
        }
    }
}
