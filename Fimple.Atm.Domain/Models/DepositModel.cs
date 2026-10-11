using System.ComponentModel.DataAnnotations;
using Fimple.Atm.Domain.Enums;
using Fimple.Atm.Domain.Validation;

namespace Fimple.Atm.Domain.Models
{
    public class DepositModel
    {
        public class DepositNoteItem
        {
            public required Currency Currency { get; set; }

            [DefinedDenomination]
            public required int Denomination { get; set; }

            [Range(1, int.MaxValue, ErrorMessage = "Note count must be positive")]
            public required int NoteCount { get; set; }
        }

        public class AcceptedNoteItem
        {
            public required Currency Currency { get; set; }
            public required int Denomination { get; set; }
            public required int NoteCount { get; set; }
            public required Guid CassetteId { get; set; }
            public required int CassettePosition { get; set; }
            public required CassetteKind CassetteKind { get; set; }
        }

        public class ReturnedNoteItem
        {
            public required Currency Currency { get; set; }
            public required int Denomination { get; set; }
            public required int NoteCount { get; set; }
        }

        public class AcceptedTotalItem
        {
            public required Currency Currency { get; set; }
            public required decimal Amount { get; set; }
        }

        public class Add
        {
            public class Request : AtmRequestBase
            {
                [Required]
                public required List<DepositNoteItem> Notes { get; set; }

                [Range(0, int.MaxValue, ErrorMessage = "Unrecognised note count cannot be negative")]
                public int UnrecognisedNoteCount { get; set; }
            }

            public class Return
            {
                public required Guid DepositId { get; set; }
                public required Guid AtmId { get; set; }
                public required List<AcceptedNoteItem> AcceptedNotes { get; set; }
                public required List<ReturnedNoteItem> ReturnedNotes { get; set; }
                public required List<AcceptedTotalItem> AcceptedTotals { get; set; }
                public required int UnrecognisedNoteCount { get; set; }
                public required int AcceptedNoteCount { get; set; }
                public required int ReturnedNoteCount { get; set; }
            }
        }
    }
}
