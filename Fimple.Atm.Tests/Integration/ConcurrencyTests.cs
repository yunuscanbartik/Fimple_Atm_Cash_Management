using System.Net;
using Fimple.Atm.Domain.Enums;
using Fimple.Atm.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Fimple.Atm.Tests.Integration
{
    [Collection(ApiCollection.NAME)]
    public class ConcurrencyTests : IntegrationTestBase
    {
        private const int PARALLEL_REQUEST_COUNT = 30;
        private const int AVAILABLE_NOTE_COUNT = 10;

        public ConcurrencyTests(ApiFixture fixture) : base(fixture)
        {

        }

        [Fact]
        public async Task ParallelQuotesNeverReserveMoreNotesThanTheCassetteHolds()
        {
            var atmId = await Atms.CreateInServiceAtm(AtmTestHelper.LargeNoteCassettes, new LoadItem(3, AVAILABLE_NOTE_COUNT));

            var responses = await Task.WhenAll(Enumerable.Range(0, PARALLEL_REQUEST_COUNT)
                .Select(_ => Atms.RequestQuote(atmId, AtmTestHelper.NewCardNumber(), Currency.Try, 100)));

            var created = responses.Count(response => response.StatusCode == HttpStatusCode.Created);
            var rejected = responses.Where(response => response.StatusCode != HttpStatusCode.Created).ToList();
            Assert.Equal(AVAILABLE_NOTE_COUNT, created);
            foreach (var response in rejected)
            {
                Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
                Assert.Equal("INSUFFICIENT_CASH", await ApiClient.ErrorCode(response));
            }

            var cassette = await Atms.GetCassette(atmId, 3);
            Assert.Equal(AVAILABLE_NOTE_COUNT, cassette.NoteCount);
            Assert.Equal(AVAILABLE_NOTE_COUNT, cassette.ReservedCount);
            Assert.Equal(0, cassette.AvailableCount);

            var reservationMovementCount = await Fixture.WithDbContext(appDbContext => appDbContext.CassetteMovements
                .CountAsync(movement => movement.Cassette.AtmId == atmId && movement.MovementType == CassetteMovementType.Reservation));
            Assert.Equal(AVAILABLE_NOTE_COUNT, reservationMovementCount);

            await Atms.AssertConsistent(atmId);
        }

        [Fact]
        public async Task TwoSimultaneousQuotesForTheLastNoteOnlyOneSucceeds()
        {
            for (var round = 0; round < 5; round++)
            {
                var atmId = await Atms.CreateInServiceAtm(AtmTestHelper.LargeNoteCassettes, new LoadItem(3, 1));

                var responses = await Task.WhenAll(
                    Atms.RequestQuote(atmId, AtmTestHelper.NewCardNumber(), Currency.Try, 100),
                    Atms.RequestQuote(atmId, AtmTestHelper.NewCardNumber(), Currency.Try, 100));

                Assert.Equal(1, responses.Count(response => response.StatusCode == HttpStatusCode.Created));
                Assert.Equal(1, responses.Count(response => response.StatusCode == HttpStatusCode.UnprocessableEntity));
            }
        }

        [Fact]
        public async Task ParallelQuotesWithTheSameCardCannotExceedTheDailyLimit()
        {
            var atmId = await Atms.CreateInServiceAtm(AtmTestHelper.LargeNoteCassettes, new LoadItem(2, 100));
            var cardNumber = AtmTestHelper.NewCardNumber();

            var responses = await Task.WhenAll(Enumerable.Range(0, 20)
                .Select(_ => Atms.RequestQuote(atmId, cardNumber, Currency.Try, 5000)));

            Assert.Equal(4, responses.Count(response => response.StatusCode == HttpStatusCode.Created));
            foreach (var response in responses.Where(response => response.StatusCode != HttpStatusCode.Created))
            {
                Assert.Equal("DAILY_LIMIT_EXCEEDED", await ApiClient.ErrorCode(response));
            }

            await Atms.AssertConsistent(atmId);
        }

        [Fact]
        public async Task ParallelConfirmsWithTheSameIdempotencyKeyDebitOnlyOnce()
        {
            var atmId = await Atms.CreateInServiceAtm(AtmTestHelper.LargeNoteCassettes, new LoadItem(3, 20));
            var quote = await Atms.CreateQuote(atmId, AtmTestHelper.NewCardNumber(), Currency.Try, 300);

            var responses = await Task.WhenAll(Enumerable.Range(0, 10)
                .Select(_ => Atms.ConfirmQuote(atmId, quote.QuoteId, "same-key")));

            var results = new List<WithdrawalModel.ConfirmQuote.Return>();
            foreach (var response in responses)
            {
                results.Add(await ApiClient.Read<WithdrawalModel.ConfirmQuote.Return>(response));
            }

            Assert.All(results, result => Assert.Equal(300m, result.Amount));
            Assert.Single(results.Select(result => result.NoteCount).Distinct());

            var cassette = await Atms.GetCassette(atmId, 3);
            Assert.Equal(17, cassette.NoteCount);
            Assert.Equal(0, cassette.ReservedCount);

            var withdrawalMovementCount = await Fixture.WithDbContext(appDbContext => appDbContext.CassetteMovements
                .CountAsync(movement => movement.ReferenceId == quote.QuoteId && movement.MovementType == CassetteMovementType.Withdrawal));
            Assert.Equal(1, withdrawalMovementCount);

            await Atms.AssertConsistent(atmId);
        }
    }
}
