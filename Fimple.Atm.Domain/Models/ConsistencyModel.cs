using Fimple.Atm.Domain.Enums;

namespace Fimple.Atm.Domain.Models
{
    public class ConsistencyModel
    {
        public class EntryItem
        {
            public required Guid CassetteId { get; set; }
            public required int CassettePosition { get; set; }
            public required Currency Currency { get; set; }
            public required int Denomination { get; set; }
            public required int StoredNoteCount { get; set; }
            public required int CalculatedNoteCount { get; set; }
            public required int StoredReservedCount { get; set; }
            public required int CalculatedReservedCount { get; set; }
            public required bool IsConsistent { get; set; }
        }

        public class Check
        {
            public class Request : AtmRequestBase
            {

            }

            public class Return
            {
                public required Guid AtmId { get; set; }
                public required DateTime CheckedAt { get; set; }
                public required bool IsConsistent { get; set; }
                public required int DifferenceCount { get; set; }
                public required List<EntryItem> Differences { get; set; }
                public required List<EntryItem> Entries { get; set; }
            }
        }
    }
}
