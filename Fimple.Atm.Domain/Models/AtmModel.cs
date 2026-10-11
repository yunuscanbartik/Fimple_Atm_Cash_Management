using System.ComponentModel.DataAnnotations;
using Fimple.Atm.Domain.Constants;
using Fimple.Atm.Domain.Enums;
using Fimple.Atm.Domain.Validation;

namespace Fimple.Atm.Domain.Models
{
    public class AtmModel
    {
        public class AtmItem
        {
            public required Guid Id { get; set; }
            public required string Name { get; set; }
            public required AtmStatus Status { get; set; }
        }

        public class CassetteContentItem
        {
            public required Currency Currency { get; set; }
            public required int Denomination { get; set; }
            public required int NoteCount { get; set; }
            public required int ReservedCount { get; set; }
            public required int AvailableCount { get; set; }
        }

        public class CassetteItem
        {
            public required Guid Id { get; set; }
            public required int Position { get; set; }
            public required CassetteKind Kind { get; set; }
            public Currency? Currency { get; set; }
            public int? Denomination { get; set; }
            public required int Capacity { get; set; }
            public required int NoteCount { get; set; }
            public required int ReservedCount { get; set; }
            public required int AvailableCount { get; set; }
            public required List<CassetteContentItem> Contents { get; set; }
        }

        public class CurrencyTotalItem
        {
            public required Currency Currency { get; set; }
            public required int NoteCount { get; set; }
            public required decimal Amount { get; set; }
        }

        public class CassetteConfigItem
        {
            [Range(1, AtmRules.CASSETTE_COUNT, ErrorMessage = "Cassette position must be between 1 and 5")]
            public required int Position { get; set; }

            public required CassetteKind Kind { get; set; }

            public Currency? Currency { get; set; }

            [DefinedDenomination]
            public int? Denomination { get; set; }
        }

        public class CashItem
        {
            [Range(1, AtmRules.CASSETTE_COUNT, ErrorMessage = "Cassette position must be between 1 and 5")]
            public required int CassettePosition { get; set; }

            public Currency? Currency { get; set; }

            [DefinedDenomination]
            public int? Denomination { get; set; }

            [Range(1, AtmRules.CASSETTE_CAPACITY, ErrorMessage = "Note count must be between 1 and the cassette capacity")]
            public required int NoteCount { get; set; }
        }

        public class Get
        {
            public class Request : UserRequestBase
            {

            }

            public class Return
            {
                public required List<AtmItem> Items { get; set; }
            }
        }

        public class GetDetail
        {
            public class Request : AtmRequestBase
            {

            }

            public class Return
            {
                public required Guid Id { get; set; }
                public required string Name { get; set; }
                public required AtmStatus Status { get; set; }
                public required List<CassetteItem> Cassettes { get; set; }
                public required List<CurrencyTotalItem> Totals { get; set; }
            }
        }

        public class Add
        {
            public class Request : UserRequestBase
            {
                [MinLength(1, ErrorMessage = "ATM name is required")]
                [MaxLength(255)]
                public required string Name { get; set; }
            }

            public class Return
            {
                public required Guid Id { get; set; }
                public required string Name { get; set; }
                public required AtmStatus Status { get; set; }
            }
        }

        public class ConfigureCassettes
        {
            public class Request : AtmRequestBase
            {
                [Required(ErrorMessage = "Cassettes are required")]
                [MinLength(AtmRules.CASSETTE_COUNT, ErrorMessage = "Exactly 5 cassettes must be provided")]
                [MaxLength(AtmRules.CASSETTE_COUNT, ErrorMessage = "Exactly 5 cassettes must be provided")]
                public required List<CassetteConfigItem> Cassettes { get; set; }
            }

            public class Return
            {
                public required Guid Id { get; set; }
                public required string Name { get; set; }
                public required AtmStatus Status { get; set; }
                public required List<CassetteItem> Cassettes { get; set; }
                public required List<CurrencyTotalItem> Totals { get; set; }
            }
        }

        public class LoadCash
        {
            public class Request : AtmRequestBase
            {
                [Required(ErrorMessage = "Items are required")]
                [MinLength(1, ErrorMessage = "At least one item is required")]
                public required List<CashItem> Items { get; set; }
            }

            public class Return
            {
                public required Guid Id { get; set; }
                public required string Name { get; set; }
                public required AtmStatus Status { get; set; }
                public required List<CassetteItem> Cassettes { get; set; }
                public required List<CurrencyTotalItem> Totals { get; set; }
            }
        }

        public class UnloadCash
        {
            public class Request : AtmRequestBase
            {
                [Required(ErrorMessage = "Items are required")]
                [MinLength(1, ErrorMessage = "At least one item is required")]
                public required List<CashItem> Items { get; set; }
            }

            public class Return
            {
                public required Guid Id { get; set; }
                public required string Name { get; set; }
                public required AtmStatus Status { get; set; }
                public required List<CassetteItem> Cassettes { get; set; }
                public required List<CurrencyTotalItem> Totals { get; set; }
            }
        }

        public class PutInService
        {
            public class Request : AtmRequestBase
            {

            }

            public class Return
            {
                public required Guid Id { get; set; }
                public required string Name { get; set; }
                public required AtmStatus Status { get; set; }
            }
        }

        public class EnterMaintenance
        {
            public class Request : AtmRequestBase
            {

            }

            public class Return
            {
                public required Guid Id { get; set; }
                public required string Name { get; set; }
                public required AtmStatus Status { get; set; }
            }
        }
    }
}
