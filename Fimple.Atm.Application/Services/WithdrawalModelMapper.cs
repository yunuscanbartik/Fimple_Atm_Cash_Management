using Fimple.Atm.Domain.Entities;
using Fimple.Atm.Domain.Enums;
using Fimple.Atm.Domain.Models;

namespace Fimple.Atm.Application.Services
{
    internal static class WithdrawalModelMapper
    {
        public static WithdrawalModel.CreateQuote.Return ToCreateQuoteReturn(WithdrawalQuote quote, IEnumerable<Cassette> cassettes, DateTime now)
        {
            var status = GetEffectiveStatus(quote, now);
            var isExactMatch = HasExactOption(quote);
            var options = ToOptionItems(quote, cassettes);

            return new WithdrawalModel.CreateQuote.Return
            {
                QuoteId = quote.Id,
                AtmId = quote.AtmId,
                Currency = quote.Currency,
                RequestedAmount = quote.RequestedAmount,
                Status = status,
                MaskedCardNumber = quote.MaskedCardNumber,
                ExpiresAt = quote.ExpiresAt,
                IsExactMatch = isExactMatch,
                Options = options
            };
        }

        public static WithdrawalModel.GetQuote.Return ToGetQuoteReturn(WithdrawalQuote quote, IEnumerable<Cassette> cassettes, DateTime now)
        {
            var status = GetEffectiveStatus(quote, now);
            var isExactMatch = HasExactOption(quote);
            var options = ToOptionItems(quote, cassettes);

            return new WithdrawalModel.GetQuote.Return
            {
                QuoteId = quote.Id,
                AtmId = quote.AtmId,
                Currency = quote.Currency,
                RequestedAmount = quote.RequestedAmount,
                Status = status,
                MaskedCardNumber = quote.MaskedCardNumber,
                ExpiresAt = quote.ExpiresAt,
                IsExactMatch = isExactMatch,
                Options = options
            };
        }

        public static WithdrawalModel.CancelQuote.Return ToCancelQuoteReturn(WithdrawalQuote quote, IEnumerable<Cassette> cassettes, DateTime now)
        {
            var status = GetEffectiveStatus(quote, now);
            var isExactMatch = HasExactOption(quote);
            var options = ToOptionItems(quote, cassettes);

            return new WithdrawalModel.CancelQuote.Return
            {
                QuoteId = quote.Id,
                AtmId = quote.AtmId,
                Currency = quote.Currency,
                RequestedAmount = quote.RequestedAmount,
                Status = status,
                MaskedCardNumber = quote.MaskedCardNumber,
                ExpiresAt = quote.ExpiresAt,
                IsExactMatch = isExactMatch,
                Options = options
            };
        }

        public static WithdrawalModel.ConfirmQuote.Return ToConfirmQuoteReturn(WithdrawalQuote quote, WithdrawalQuoteOption confirmedOption, IEnumerable<Cassette> cassettes)
        {
            var optionItem = ToOptionItem(confirmedOption, CassettePositions.Build(cassettes));

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

        private static List<WithdrawalModel.OptionItem> ToOptionItems(WithdrawalQuote quote, IEnumerable<Cassette> cassettes)
        {
            var positions = CassettePositions.Build(cassettes);
            return quote.Options
                .OrderBy(option => option.OptionType)
                .Select(option => ToOptionItem(option, positions))
                .ToList();
        }

        private static WithdrawalModel.OptionItem ToOptionItem(WithdrawalQuoteOption option, Dictionary<Guid, int> positions)
        {
            var notes = option.Items
                .OrderBy(item => CassettePositions.Get(positions, item.CassetteId))
                .Select(item => ToNoteItem(item, positions))
                .ToList();
            var noteCount = notes.Sum(note => note.NoteCount);

            return new WithdrawalModel.OptionItem
            {
                OptionType = option.OptionType,
                Amount = option.Amount,
                NoteCount = noteCount,
                Notes = notes
            };
        }

        private static WithdrawalModel.NoteItem ToNoteItem(WithdrawalQuoteOptionItem item, Dictionary<Guid, int> positions)
        {
            var cassettePosition = CassettePositions.Get(positions, item.CassetteId);
            var denomination = (int)item.Denomination;

            return new WithdrawalModel.NoteItem
            {
                CassetteId = item.CassetteId,
                CassettePosition = cassettePosition,
                Denomination = denomination,
                NoteCount = item.NoteCount
            };
        }
    }
}
