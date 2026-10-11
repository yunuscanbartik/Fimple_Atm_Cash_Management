using System.Net;
using Fimple.Atm.Domain.Enums;

namespace Fimple.Atm.Tests.Integration
{
    [Collection(ApiCollection.NAME)]
    public class AtmLifecycleTests : IntegrationTestBase
    {
        public AtmLifecycleTests(ApiFixture fixture) : base(fixture)
        {

        }

        [Fact]
        public async Task UnconfiguredAtmCannotBeLoadedOrPutInService()
        {
            var atmId = await Atms.CreateAtm();

            var load = await Api.Post($"/api/atms/{atmId}/load", new { items = new[] { new LoadItem(2, 5) } });
            var putInService = await Api.Post($"/api/atms/{atmId}/put-in-service");

            Assert.Equal(HttpStatusCode.Conflict, load.StatusCode);
            Assert.Equal("INVALID_ATM_STATE", await ApiClient.ErrorCode(load));
            Assert.Equal(HttpStatusCode.Conflict, putInService.StatusCode);
        }

        [Fact]
        public async Task WithdrawalsAndDepositsAreOnlyAllowedInService()
        {
            var atmId = await Atms.CreateConfiguredAtm(AtmTestHelper.StandardCassettes);
            await ApiClient.EnsureSuccess(await Api.Post($"/api/atms/{atmId}/load", new { items = new[] { new LoadItem(2, 5) } }));

            var quote = await Atms.RequestQuote(atmId, AtmTestHelper.NewCardNumber(), Currency.Try, 100);
            var deposit = await Api.Post($"/api/atms/{atmId}/deposits", new { notes = new[] { new { currency = Currency.Try, denomination = 100, noteCount = 1 } } });

            Assert.Equal(HttpStatusCode.Conflict, quote.StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, deposit.StatusCode);
        }

        [Fact]
        public async Task CassetteChangesAreRejectedWhileInService()
        {
            var atmId = await Atms.CreateInServiceAtm(AtmTestHelper.StandardCassettes, new LoadItem(2, 5));

            var configure = await Api.Put($"/api/atms/{atmId}/cassettes", new { cassettes = AtmTestHelper.StandardCassettes });
            var load = await Api.Post($"/api/atms/{atmId}/load", new { items = new[] { new LoadItem(2, 1) } });
            var unload = await Api.Post($"/api/atms/{atmId}/unload", new { items = new[] { new LoadItem(2, 1) } });

            Assert.Equal(HttpStatusCode.Conflict, configure.StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, load.StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, unload.StatusCode);
        }

        [Fact]
        public async Task InvalidCassetteConfigurationIsRejectedWithAMeaningfulCode()
        {
            var atmId = await Atms.CreateAtm();
            var withoutForeignCurrency = new[]
            {
                new CassetteSpec(1, CassetteKind.Deposit),
                new CassetteSpec(2, CassetteKind.Recycling, Currency.Try, 100),
                new CassetteSpec(3, CassetteKind.Recycling, Currency.Try, 50),
                new CassetteSpec(4, CassetteKind.Recycling, Currency.Try, 20),
                new CassetteSpec(5, CassetteKind.Recycling, Currency.Try, 500)
            };

            var response = await Api.Put($"/api/atms/{atmId}/cassettes", new { cassettes = withoutForeignCurrency });

            Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
            Assert.Equal("INVALID_CASSETTE_CONFIGURATION", await ApiClient.ErrorCode(response));
        }

        [Fact]
        public async Task LoadingBeyondCapacityOrWithTheWrongNoteIsRejected()
        {
            var atmId = await Atms.CreateConfiguredAtm(AtmTestHelper.StandardCassettes);
            await ApiClient.EnsureSuccess(await Api.Post($"/api/atms/{atmId}/load", new { items = new[] { new LoadItem(2, 100) } }));

            var overCapacity = await Api.Post($"/api/atms/{atmId}/load", new { items = new[] { new LoadItem(2, 1) } });
            var wrongNote = await Api.Post($"/api/atms/{atmId}/load", new { items = new[] { new LoadItem(2, 1, Currency.Usd) } });

            Assert.Equal("CASSETTE_CAPACITY_EXCEEDED", await ApiClient.ErrorCode(overCapacity));
            Assert.Equal("CASSETTE_NOTE_MISMATCH", await ApiClient.ErrorCode(wrongNote));
        }

        [Fact]
        public async Task UnknownAtmReturnsNotFoundProblemDetails()
        {
            var response = await Api.Get($"/api/atms/{Guid.NewGuid()}");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
            Assert.Equal("ATM_NOT_FOUND", await ApiClient.ErrorCode(response));
        }
    }
}
