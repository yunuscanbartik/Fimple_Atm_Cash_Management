using Fimple.Atm.Domain.Entities;
using Fimple.Atm.Domain.Enums;
using Fimple.Atm.Domain.Models;

namespace Fimple.Atm.Application.Services
{
    internal static class WithdrawalModelMapper
    {
        public static WithdrawalModel.QuoteItem ToQuoteItem(WithdrawalQuote quote, IEnumerable<Cassette> cassettes, DateTime now)
        {
            var positions = cassettes.ToDictionary(cassette => cassette.Id, cassette => cassette.Position);
            var options = quote.Options
                .OrderBy(option => option.OptionType)
                .Select(option => ToOptionItem(option, positions))
                .ToList();

            return new WithdrawalModel.QuoteItem
            {
                QuoteId = quote.Id,
                AtmId = quote.AtmId,
                Currency = quote.Currency,
                RequestedAmount = quote.RequestedAmount,
                Status = GetEffectiveStatus(quote, now),
                MaskedCardNumber = quote.MaskedCardNumber,
                ExpiresAt = quote.ExpiresAt,
                IsExactMatch = quote.Options.Any(option => option.OptionType == QuoteOptionType.Exact),
                Options = options
            };
        }

        public static WithdrawalModel.ConfirmItem ToConfirmItem(WithdrawalQuote quote, WithdrawalQuoteOption confirmedOption, IEnumerable<Cassette> cassettes)
        {
            var positions = cassettes.ToDictionary(cassette => cassette.Id, cassette => cassette.Position);
            var optionItem = ToOptionItem(confirmedOption, positions);

            return new WithdrawalModel.ConfirmItem
            {
                QuoteId = quote.Id,
                AtmId = quote.AtmId,
                Status = quote.Status,
                Currency = quote.Currency,
                OptionType = optionItem.OptionType,
                Amount = optionItem.Amount,
                NoteCount = optionItem.NoteCount,
                Notes = optionItem.Notes
            };
        }

        public static QuoteStatus GetEffectiveStatus(WithdrawalQuote quote, DateTime now)
        {
            var isLapsed = quote.Status == QuoteStatus.Pending && quote.ExpiresAt <= now;
            return isLapsed ? QuoteStatus.Expired : quote.Status;
        }

        private static WithdrawalModel.OptionItem ToOptionItem(WithdrawalQuoteOption option, Dictionary<Guid, int> positions)
        {
            var notes = option.Items
                .OrderBy(item => positions.GetValueOrDefault(item.CassetteId))
                .Select(item => new WithdrawalModel.NoteItem
                {
                    CassetteId = item.CassetteId,
                    CassettePosition = positions.GetValueOrDefault(item.CassetteId),
                    Denomination = (int)item.Denomination,
                    NoteCount = item.NoteCount
                })
                .ToList();

            return new WithdrawalModel.OptionItem
            {
                OptionType = option.OptionType,
                Amount = option.Amount,
                NoteCount = notes.Sum(note => note.NoteCount),
                Notes = notes
            };
        }
    }
}
