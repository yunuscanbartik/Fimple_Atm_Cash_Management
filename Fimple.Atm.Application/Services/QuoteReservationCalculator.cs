using Fimple.Atm.Domain.Entities;
using Fimple.Atm.Domain.Enums;

namespace Fimple.Atm.Application.Services
{
    public static class QuoteReservationCalculator
    {
        public static Dictionary<(Guid CassetteId, Denomination Denomination), int> GetReservedCounts(WithdrawalQuote quote)
        {
            var reservedCounts = new Dictionary<(Guid CassetteId, Denomination Denomination), int>();

            foreach (var option in quote.Options)
            {
                var optionCounts = option.Items
                    .GroupBy(item => (item.CassetteId, item.Denomination))
                    .ToDictionary(group => group.Key, group => group.Sum(item => item.NoteCount));

                foreach (var (key, noteCount) in optionCounts)
                {
                    var alreadyReserved = reservedCounts.GetValueOrDefault(key);
                    reservedCounts[key] = Math.Max(alreadyReserved, noteCount);
                }
            }

            return reservedCounts;
        }

        public static Dictionary<(Guid CassetteId, Denomination Denomination), int> GetOptionCounts(WithdrawalQuoteOption option)
        {
            return option.Items
                .GroupBy(item => (item.CassetteId, item.Denomination))
                .ToDictionary(group => group.Key, group => group.Sum(item => item.NoteCount));
        }
    }
}
