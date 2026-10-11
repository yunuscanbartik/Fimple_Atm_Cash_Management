using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Fimple.Atm.Domain.Constants;
using Fimple.Atm.Domain.Enums;
using Fimple.Atm.Domain.Validation;

namespace Fimple.Atm.Domain.Models
{
    public class ReconciliationModel
    {
        public class CountItem
        {
            [Range(1, AtmRules.CassetteCount, ErrorMessage = "Cassette position must be between 1 and 5")]
            public required int CassettePosition { get; set; }

            public Currency? Currency { get; set; }

            [DefinedDenomination]
            public int? Denomination { get; set; }

            [Range(0, int.MaxValue, ErrorMessage = "Counted note count cannot be negative")]
            public required int CountedNoteCount { get; set; }
        }

        public class LineItem
        {
            public required Guid CassetteId { get; set; }
            public required int CassettePosition { get; set; }
            public required Currency Currency { get; set; }
            public required int Denomination { get; set; }
            public required int SystemCount { get; set; }
            public required int CountedCount { get; set; }
            public required int Difference { get; set; }
        }

        public class ReconciliationItem
        {
            public required Guid Id { get; set; }
            public required Guid AtmId { get; set; }
            public required ReconciliationStatus Status { get; set; }
            public required decimal TotalDifferenceInBaseCurrency { get; set; }
            public required bool RequiresApproval { get; set; }
            public required string CreatedBy { get; set; }
            public required DateTime CreatedAt { get; set; }
            public string? ApprovedBy { get; set; }
            public DateTime? ApprovedAt { get; set; }
            public required List<LineItem> Lines { get; set; }
        }

        public class Get
        {
            public class Request : AtmRequestBase
            {

            }

            public class Return
            {
                public required List<ReconciliationItem> Items { get; set; }
            }
        }

        public class GetDetail
        {
            public class Request : AtmRequestBase
            {
                [JsonIgnore]
                public Guid ReconciliationId { get; set; }
            }

            public class Return
            {
                public required Guid Id { get; set; }
                public required Guid AtmId { get; set; }
                public required ReconciliationStatus Status { get; set; }
                public required decimal TotalDifferenceInBaseCurrency { get; set; }
                public required bool RequiresApproval { get; set; }
                public required string CreatedBy { get; set; }
                public required DateTime CreatedAt { get; set; }
                public string? ApprovedBy { get; set; }
                public DateTime? ApprovedAt { get; set; }
                public required List<LineItem> Lines { get; set; }
            }
        }

        public class Add
        {
            public class Request : AtmRequestBase
            {
                [Required(ErrorMessage = "Counts are required")]
                [MinLength(1, ErrorMessage = "At least one count is required")]
                public required List<CountItem> Counts { get; set; }
            }

            public class Return
            {
                public required Guid Id { get; set; }
                public required Guid AtmId { get; set; }
                public required ReconciliationStatus Status { get; set; }
                public required decimal TotalDifferenceInBaseCurrency { get; set; }
                public required bool RequiresApproval { get; set; }
                public required string CreatedBy { get; set; }
                public required DateTime CreatedAt { get; set; }
                public string? ApprovedBy { get; set; }
                public DateTime? ApprovedAt { get; set; }
                public required List<LineItem> Lines { get; set; }
            }
        }

        public class Approve
        {
            public class Request : AtmRequestBase
            {
                [JsonIgnore]
                public Guid ReconciliationId { get; set; }
            }

            public class Return
            {
                public required Guid Id { get; set; }
                public required Guid AtmId { get; set; }
                public required ReconciliationStatus Status { get; set; }
                public required decimal TotalDifferenceInBaseCurrency { get; set; }
                public required bool RequiresApproval { get; set; }
                public required string CreatedBy { get; set; }
                public required DateTime CreatedAt { get; set; }
                public string? ApprovedBy { get; set; }
                public DateTime? ApprovedAt { get; set; }
                public required List<LineItem> Lines { get; set; }
            }
        }
    }
}
