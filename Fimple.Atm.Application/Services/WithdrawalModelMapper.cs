using Fimple.Atm.Domain.Entities;
using Fimple.Atm.Domain.Enums;
using Fimple.Atm.Domain.Models;

namespace Fimple.Atm.Application.Services
{
    internal static class WithdrawalModelMapper
    {
        public static WithdrawalModel.CreateQuote.Return ToCreateQuoteReturn(WithdrawalQuote quote, IEnumerable<Cassette> cassettes, DateTime now)
        {
            return new WithdrawalModel.CreateQuote.Return
            {
                QuoteId = quote.Id,
                AtmId = quote.AtmId,
                Currency = quote.Currency,
                RequestedAmount = quote.RequestedAmount,
                Status = GetEffectiveStatus(quote, now),
                MaskedCardNumber = quote.MaskedCardNumber,
                ExpiresAt = quote.ExpiresAt,
                IsExactMatch = HasExactOption(quote),
                Options = ToOptionItems(quote, cassettes)
            };
        }

        public static WithdrawalModel.GetQuote.Return ToGetQuoteReturn(WithdrawalQuote quote, IEnumerable<Cassette> cassettes, DateTime now)
        {
            return new WithdrawalModel.GetQuote.Return
            {
                QuoteId = quote.Id,
                AtmId = quote.AtmId,
                Currency = quote.Currency,
                RequestedAmount = quote.RequestedAmount,
                Status = GetEffectiveStatus(quote, now),
                MaskedCardNumber = quote.MaskedCardNumber,
                ExpiresAt = quote.ExpiresAt,
                IsExactMatch = HasExactOption(quote),
                Options = ToOptionItems(quote, cassettes)
            };
        }

        public static WithdrawalModel.CancelQuote.Return ToCancelQuoteReturn(WithdrawalQuote quote, IEnumerable<Cassette> cassettes, DateTime now)
        {
            return new WithdrawalModel.CancelQuote.Return
            {
                QuoteId = quote.Id,
                AtmId = quote.AtmId,
                Currency = quote.Currency,
                RequestedAmount = quote.RequestedAmount,
                Status = GetEffectiveStatus(quote, now),
                MaskedCardNumber = quote.MaskedCardNumber,
                ExpiresAt = quote.ExpiresAt,
                IsExactMatch = HasExactOption(quote),
                Options = ToOptionItems(quote, cassettes)
            };
        }

        public static WithdrawalModel.ConfirmQuote.Return ToConfirmQuoteReturn(WithdrawalQuote quote, WithdrawalQuoteOption confirmedOption, IEnumerable<Cassette> cassettes)
        {
            var optionItem = ToOptionItem(confirmedOption, ToPositions(cassettes));

            return new WithdrawalModel.ConfirmQuote.Return
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

        private static bool HasExactOption(WithdrawalQuote quote)
        {
            return quote.Options.Any(option => option.OptionType == QuoteOptionType.Exact);
        }

        private static Dictionary<Guid, int> ToPositions(IEnumerable<Cassette> cassettes)
        {
            return cassettes.ToDictionary(cassette => cassette.Id, cassette => cassette.Position);
        }

        private static List<WithdrawalModel.OptionItem> ToOptionItems(WithdrawalQuote quote, IEnumerable<Cassette> cassettes)
        {
            var positions = ToPositions(cassettes);
            return quote.Options
                .OrderBy(option => option.OptionType)
                .Select(option => ToOptionItem(option, positions))
                .ToList();
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
