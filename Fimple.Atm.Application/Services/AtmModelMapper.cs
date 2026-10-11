using Fimple.Atm.Domain.Entities;
using Fimple.Atm.Domain.Enums;
using Fimple.Atm.Domain.Models;

namespace Fimple.Atm.Application.Services
{
    internal static class AtmModelMapper
    {
        public static AtmModel.GetDetail.Return ToGetDetailReturn(AtmMachine atm)
        {
            var cassetteItems = ToCassetteItems(atm);
            var currencyTotals = ToCurrencyTotals(atm);

            return new AtmModel.GetDetail.Return
            {
                Id = atm.Id,
                Name = atm.Name,
                Status = atm.Status,
                Cassettes = cassetteItems,
                Totals = currencyTotals
            };
        }

        public static AtmModel.ConfigureCassettes.Return ToConfigureCassettesReturn(AtmMachine atm)
        {
            var cassetteItems = ToCassetteItems(atm);
            var currencyTotals = ToCurrencyTotals(atm);

            return new AtmModel.ConfigureCassettes.Return
            {
                Id = atm.Id,
                Name = atm.Name,
                Status = atm.Status,
                Cassettes = cassetteItems,
                Totals = currencyTotals
            };
        }

        public static AtmModel.LoadCash.Return ToLoadCashReturn(AtmMachine atm)
        {
            var cassetteItems = ToCassetteItems(atm);
            var currencyTotals = ToCurrencyTotals(atm);

            return new AtmModel.LoadCash.Return
            {
                Id = atm.Id,
                Name = atm.Name,
                Status = atm.Status,
                Cassettes = cassetteItems,
                Totals = currencyTotals
            };
        }

        public static AtmModel.UnloadCash.Return ToUnloadCashReturn(AtmMachine atm)
        {
            var cassetteItems = ToCassetteItems(atm);
            var currencyTotals = ToCurrencyTotals(atm);

            return new AtmModel.UnloadCash.Return
            {
                Id = atm.Id,
                Name = atm.Name,
                Status = atm.Status,
                Cassettes = cassetteItems,
                Totals = currencyTotals
            };
        }

        public static List<AtmModel.CassetteItem> ToCassetteItems(AtmMachine atm)
        {
            return atm.Cassettes
                .OrderBy(cassette => cassette.Position)
                .Select(ToCassetteItem)
                .ToList();
        }

        public static List<AtmModel.CurrencyTotalItem> ToCurrencyTotals(AtmMachine atm)
        {
            var contents = atm.Cassettes.SelectMany(cassette => cassette.Contents).ToList();
            return Enum.GetValues<Currency>()
                .Select(currency =>
                {
                    var currencyContents = contents.Where(content => content.Currency == currency).ToList();
                    var noteCount = currencyContents.Sum(content => content.NoteCount);
                    var amount = currencyContents.Sum(content => (decimal)content.NoteCount * (int)content.Denomination);
                    return new AtmModel.CurrencyTotalItem
                    {
                        Currency = currency,
                        NoteCount = noteCount,
                        Amount = amount
                    };
                })
                .ToList();
        }

        private static AtmModel.CassetteItem ToCassetteItem(Cassette cassette)
        {
            var noteCount = cassette.Contents.Sum(content => content.NoteCount);
            var reservedCount = cassette.Contents.Sum(content => content.ReservedCount);
            var denomination = cassette.Denomination.HasValue ? (int?)cassette.Denomination.Value : null;
            var contents = cassette.Contents
                .OrderBy(content => content.Currency)
                .ThenBy(content => content.Denomination)
                .Select(content => new AtmModel.CassetteContentItem
                {
                    Currency = content.Currency,
                    Denomination = (int)content.Denomination,
                    NoteCount = content.NoteCount,
                    ReservedCount = content.ReservedCount,
                    AvailableCount = content.AvailableCount
                })
                .ToList();

            return new AtmModel.CassetteItem
            {
                Id = cassette.Id,
                Position = cassette.Position,
                Kind = cassette.Kind,
                Currency = cassette.Currency,
                Denomination = denomination,
                Capacity = cassette.Capacity,
                NoteCount = noteCount,
                ReservedCount = reservedCount,
                AvailableCount = noteCount - reservedCount,
                Contents = contents
            };
        }
    }
}
