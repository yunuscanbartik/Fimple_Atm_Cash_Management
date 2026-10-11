using Fimple.Atm.Domain.Enums;

namespace Fimple.Atm.Domain.Models
{
    public class ReportModel
    {
        public class CurrencyTotalItem
        {
            public required Currency Currency { get; set; }
            public required int NoteCount { get; set; }
            public required int ReservedCount { get; set; }
            public required int AvailableCount { get; set; }
            public required decimal Amount { get; set; }
        }

        public class AtmReportItem
        {
            public required Guid AtmId { get; set; }
            public required string Name { get; set; }
            public required AtmStatus Status { get; set; }
            public required List<AtmModel.CassetteItem> Cassettes { get; set; }
            public required List<CurrencyTotalItem> Totals { get; set; }
            public required decimal TotalInBaseCurrency { get; set; }
        }

        public class Operations
        {
            public class Request : UserRequestBase
            {
                public Guid? AtmId { get; set; }
            }

            public class Return
            {
                public required DateTime GeneratedAt { get; set; }
                public required Currency BaseCurrency { get; set; }
                public required List<AtmReportItem> Atms { get; set; }
                public required List<CurrencyTotalItem> Totals { get; set; }
                public required decimal TotalInBaseCurrency { get; set; }
            }
        }
    }
}
