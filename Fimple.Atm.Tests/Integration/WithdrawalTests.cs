using System.Net;
using Fimple.Atm.Domain.Enums;
using Fimple.Atm.Domain.Interfaces;
using Fimple.Atm.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Fimple.Atm.Tests.Integration
{
    [Collection(ApiCollection.Name)]
    public class WithdrawalTests : IntegrationTestBase
    {
        private static readonly TimeSpan PastQuoteValidity = TimeSpan.FromSeconds(61);

        public WithdrawalTests(ApiFixture fixture) : base(fixture)
        {

        }

        [Fact]
        public async Task QuoteReservesNotesAndConfirmDebitsThem()
        {
            var atmId = await Atms.CreateInServiceAtm(AtmTestHelper.StandardCassettes, new LoadItem(2, 5), new LoadItem(3, 5), new LoadItem(4, 5));

            var quote = await Atms.CreateQuote(atmId, AtmTestHelper.NewCardNumber(), Currency.Try, 170);

            Assert.Equal(QuoteStatus.Pending, quote.Status);
            var option = Assert.Single(quote.Options);
            Assert.Equal(QuoteOptionType.Exact, option.OptionType);
            Assert.Equal(3, option.NoteCount);
            var reservedCassette = await Atms.GetCassette(atmId, 2);
            Assert.Equal(1, reservedCassette.ReservedCount);

            var confirm = await ApiClient.Read<WithdrawalModel.ConfirmItem>(await Atms.ConfirmQuote(atmId, quote.QuoteId, "debit-key"));

            Assert.Equal(170m, confirm.Amount);
            var debitedCassette = await Atms.GetCassette(atmId, 2);
            Assert.Equal(4, debitedCassette.NoteCount);
            Assert.Equal(0, debitedCassette.ReservedCount);
            await Atms.AssertConsistent(atmId);
        }

        [Fact]
        public async Task NoExactMatchOffersLowerAndUpperAndConfirmReleasesTheUnusedReservation()
        {
            var atmId = await Atms.CreateInServiceAtm(AtmTestHelper.StandardCassettes, new LoadItem(3, 1), new LoadItem(4, 2));

            var quote = await Atms.CreateQuote(atmId, AtmTestHelper.NewCardNumber(), Currency.Try, 80);

            Assert.False(quote.IsExactMatch);
            Assert.Equal(70m, quote.Options.Single(option => option.OptionType == QuoteOptionType.Lower).Amount);
            Assert.Equal(90m, quote.Options.Single(option => option.OptionType == QuoteOptionType.Upper).Amount);

            var withoutChoice = await Atms.ConfirmQuote(atmId, quote.QuoteId, "choice-key");
            Assert.Equal(HttpStatusCode.UnprocessableEntity, withoutChoice.StatusCode);
            Assert.Equal("QUOTE_OPTION_REQUIRED", await ApiClient.ErrorCode(withoutChoice));

            var confirm = await ApiClient.Read<WithdrawalModel.ConfirmItem>(await Atms.ConfirmQuote(atmId, quote.QuoteId, "choice-key", QuoteOptionType.Lower));

            Assert.Equal(70m, confirm.Amount);
            var twentyCassette = await Atms.GetCassette(atmId, 4);
            Assert.Equal(1, twentyCassette.NoteCount);
            Assert.Equal(0, twentyCassette.ReservedCount);
            await Atms.AssertConsistent(atmId);
        }

        [Fact]
        public async Task ReservedNotesAreNotOfferedInAnotherQuoteUntilTheFirstIsCancelled()
        {
            var atmId = await Atms.CreateInServiceAtm(AtmTestHelper.StandardCassettes, new LoadItem(2, 1));
            var firstQuote = await Atms.CreateQuote(atmId, AtmTestHelper.NewCardNumber(), Currency.Try, 100);

            var blocked = await Atms.RequestQuote(atmId, AtmTestHelper.NewCardNumber(), Currency.Try, 100);
            Assert.Equal(HttpStatusCode.UnprocessableEntity, blocked.StatusCode);

            var cancelled = await ApiClient.Read<WithdrawalModel.QuoteItem>(
                await Api.Post($"/api/atms/{atmId}/withdrawals/quotes/{firstQuote.QuoteId}/cancel"));
            Assert.Equal(QuoteStatus.Cancelled, cancelled.Status);

            var secondQuote = await Atms.RequestQuote(atmId, AtmTestHelper.NewCardNumber(), Currency.Try, 100);
            Assert.Equal(HttpStatusCode.Created, secondQuote.StatusCode);
        }

        [Fact]
        public async Task CancelledQuoteCannotBeConfirmed()
        {
            var atmId = await Atms.CreateInServiceAtm(AtmTestHelper.StandardCassettes, new LoadItem(2, 3));
            var quote = await Atms.CreateQuote(atmId, AtmTestHelper.NewCardNumber(), Currency.Try, 100);
            await ApiClient.EnsureSuccess(await Api.Post($"/api/atms/{atmId}/withdrawals/quotes/{quote.QuoteId}/cancel"));

            var confirm = await Atms.ConfirmQuote(atmId, quote.QuoteId, "cancelled-key");

            Assert.Equal(HttpStatusCode.Conflict, confirm.StatusCode);
            Assert.Equal("QUOTE_NOT_CONFIRMABLE", await ApiClient.ErrorCode(confirm));
            Assert.Equal(0, (await Atms.GetCassette(atmId, 2)).ReservedCount);
        }

        [Fact]
        public async Task ConfirmingAnExpiredQuoteIsRejectedAndReleasesTheReservation()
        {
            var atmId = await Atms.CreateInServiceAtm(AtmTestHelper.StandardCassettes, new LoadItem(2, 3));
            var quote = await Atms.CreateQuote(atmId, AtmTestHelper.NewCardNumber(), Currency.Try, 100);
            Fixture.Clock.Advance(PastQuoteValidity);

            var confirm = await Atms.ConfirmQuote(atmId, quote.QuoteId, "expired-key");

            Assert.Equal(HttpStatusCode.Conflict, confirm.StatusCode);
            Assert.Equal("QUOTE_EXPIRED", await ApiClient.ErrorCode(confirm));
            var cassette = await Atms.GetCassette(atmId, 2);
            Assert.Equal(3, cassette.NoteCount);
            Assert.Equal(0, cassette.ReservedCount);
            await Atms.AssertConsistent(atmId);
        }

        [Fact]
        public async Task ExpirySweepReleasesTheReservationOfAnExpiredQuote()
        {
            var atmId = await Atms.CreateInServiceAtm(AtmTestHelper.StandardCassettes, new LoadItem(2, 3));
            var quote = await Atms.CreateQuote(atmId, AtmTestHelper.NewCardNumber(), Currency.Try, 100);
            Assert.Equal(1, (await Atms.GetCassette(atmId, 2)).ReservedCount);
            Fixture.Clock.Advance(PastQuoteValidity);

            var releasedCount = await Fixture.WithScopedService<IQuoteExpiryService, int>(service => service.ReleaseExpiredQuotes());

            Assert.True(releasedCount >= 1);
            var expired = await ApiClient.Read<WithdrawalModel.QuoteItem>(await Api.Get($"/api/atms/{atmId}/withdrawals/quotes/{quote.QuoteId}"));
            Assert.Equal(QuoteStatus.Expired, expired.Status);
            Assert.Equal(0, (await Atms.GetCassette(atmId, 2)).ReservedCount);
            await Atms.AssertConsistent(atmId);
        }

        [Fact]
        public async Task ConfirmRequiresAnIdempotencyKey()
        {
            var atmId = await Atms.CreateInServiceAtm(AtmTestHelper.StandardCassettes, new LoadItem(2, 3));
            var quote = await Atms.CreateQuote(atmId, AtmTestHelper.NewCardNumber(), Currency.Try, 100);

            var response = await Api.Post($"/api/atms/{atmId}/withdrawals/quotes/{quote.QuoteId}/confirm");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("IDEMPOTENCY_KEY_REQUIRED", await ApiClient.ErrorCode(response));
        }

        [Fact]
        public async Task RepeatedConfirmReturnsTheSameResultAndDebitsOnce()
        {
            var atmId = await Atms.CreateInServiceAtm(AtmTestHelper.StandardCassettes, new LoadItem(2, 5));
            var quote = await Atms.CreateQuote(atmId, AtmTestHelper.NewCardNumber(), Currency.Try, 200);

            var first = await ApiClient.Read<WithdrawalModel.ConfirmItem>(await Atms.ConfirmQuote(atmId, quote.QuoteId, "repeat-key"));
            var second = await ApiClient.Read<WithdrawalModel.ConfirmItem>(await Atms.ConfirmQuote(atmId, quote.QuoteId, "repeat-key"));

            Assert.Equal(first.Amount, second.Amount);
            Assert.Equal(first.NoteCount, second.NoteCount);
            Assert.Equal(3, (await Atms.GetCassette(atmId, 2)).NoteCount);

            var otherKey = await Atms.ConfirmQuote(atmId, quote.QuoteId, "another-key");
            Assert.Equal(HttpStatusCode.Conflict, otherKey.StatusCode);
        }

        [Fact]
        public async Task AnIdempotencyKeyCannotBeReusedForADifferentQuote()
        {
            var atmId = await Atms.CreateInServiceAtm(AtmTestHelper.StandardCassettes, new LoadItem(2, 10));
            var firstQuote = await Atms.CreateQuote(atmId, AtmTestHelper.NewCardNumber(), Currency.Try, 100);
            var secondQuote = await Atms.CreateQuote(atmId, AtmTestHelper.NewCardNumber(), Currency.Try, 100);
            await ApiClient.EnsureSuccess(await Atms.ConfirmQuote(atmId, firstQuote.QuoteId, "shared-key"));

            var response = await Atms.ConfirmQuote(atmId, secondQuote.QuoteId, "shared-key");

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            Assert.Equal("IDEMPOTENCY_KEY_REUSED", await ApiClient.ErrorCode(response));
        }

        [Fact]
        public async Task DailyLimitIsTrackedPerCurrencyAndCountsReservedAmounts()
        {
            var atmId = await Atms.CreateInServiceAtm(AtmTestHelper.LargeNoteCassettes,
                new LoadItem(2, 100), new LoadItem(3, 50), new LoadItem(4, 5));
            var cardNumber = AtmTestHelper.NewCardNumber();

            var reserved = await Atms.CreateQuote(atmId, cardNumber, Currency.Try, 15000);
            var overLimit = await Atms.RequestQuote(atmId, cardNumber, Currency.Try, 6000);
            Assert.Equal(HttpStatusCode.UnprocessableEntity, overLimit.StatusCode);
            Assert.Equal("DAILY_LIMIT_EXCEEDED", await ApiClient.ErrorCode(overLimit));

            var otherCurrency = await Atms.RequestQuote(atmId, cardNumber, Currency.Usd, 100);
            Assert.Equal(HttpStatusCode.Created, otherCurrency.StatusCode);

            await ApiClient.EnsureSuccess(await Api.Post($"/api/atms/{atmId}/withdrawals/quotes/{reserved.QuoteId}/cancel"));
            var afterCancel = await Atms.RequestQuote(atmId, cardNumber, Currency.Try, 6000);
            Assert.Equal(HttpStatusCode.Created, afterCancel.StatusCode);
        }

        [Fact]
        public async Task ConfirmedWithdrawalsUseUpTheDailyLimit()
        {
            var atmId = await Atms.CreateInServiceAtm(AtmTestHelper.LargeNoteCassettes, new LoadItem(2, 100), new LoadItem(3, 50));
            var cardNumber = AtmTestHelper.NewCardNumber();

            var quote = await Atms.CreateQuote(atmId, cardNumber, Currency.Try, 20000);
            await ApiClient.EnsureSuccess(await Atms.ConfirmQuote(atmId, quote.QuoteId, "limit-key"));

            var next = await Atms.RequestQuote(atmId, cardNumber, Currency.Try, 100);

            Assert.Equal(HttpStatusCode.UnprocessableEntity, next.StatusCode);
            Assert.Equal("DAILY_LIMIT_EXCEEDED", await ApiClient.ErrorCode(next));
        }

        [Fact]
        public async Task CardNumbersAreNeverStoredInPlainText()
        {
            var atmId = await Atms.CreateInServiceAtm(AtmTestHelper.StandardCassettes, new LoadItem(2, 3));
            var cardNumber = AtmTestHelper.NewCardNumber();
            await Atms.CreateQuote(atmId, cardNumber, Currency.Try, 100);

            var storedQuote = await Fixture.WithDbContext(appDbContext => appDbContext.WithdrawalQuotes.SingleAsync(quote => quote.AtmId == atmId));

            Assert.NotEqual(cardNumber, storedQuote.CardHash);
            Assert.DoesNotContain(cardNumber, storedQuote.CardHash);
            Assert.DoesNotContain(cardNumber, storedQuote.MaskedCardNumber);
            Assert.EndsWith(cardNumber[^4..], storedQuote.MaskedCardNumber);
            Assert.Equal(64, storedQuote.CardHash.Length);
        }
    }
}
