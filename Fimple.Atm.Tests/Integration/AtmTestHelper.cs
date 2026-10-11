using System.Net;
using Fimple.Atm.Domain.Enums;
using Fimple.Atm.Domain.Models;

namespace Fimple.Atm.Tests.Integration
{
    public record CassetteSpec(int Position, CassetteKind Kind, Currency? Currency = null, int? Denomination = null);

    public record LoadItem(int CassettePosition, int NoteCount, Currency? Currency = null, int? Denomination = null);

    public class AtmTestHelper
    {
        public const string UserHeader = "X-User";
        public const string IdempotencyHeader = "Idempotency-Key";

        public static readonly CassetteSpec[] StandardCassettes =
        {
            new(1, CassetteKind.Deposit),
            new(2, CassetteKind.Recycling, Currency.Try, 100),
            new(3, CassetteKind.Recycling, Currency.Try, 50),
            new(4, CassetteKind.Recycling, Currency.Try, 20),
            new(5, CassetteKind.Recycling, Currency.Usd, 100)
        };

        public static readonly CassetteSpec[] LargeNoteCassettes =
        {
            new(1, CassetteKind.Deposit),
            new(2, CassetteKind.Recycling, Currency.Try, 500),
            new(3, CassetteKind.Recycling, Currency.Try, 100),
            new(4, CassetteKind.Recycling, Currency.Usd, 100),
            new(5, CassetteKind.Recycling, Currency.Eur, 50)
        };

        private readonly ApiClient _api;

        public AtmTestHelper(ApiClient api)
        {
            _api = api;
        }

        public static string NewCardNumber()
        {
            return string.Concat(Enumerable.Range(0, 16).Select(_ => Random.Shared.Next(0, 10)));
        }

        public async Task<Guid> CreateAtm()
        {
            var response = await _api.Post("/api/atms", new { name = $"Test-ATM-{Guid.NewGuid():N}" });
            var atm = await ApiClient.Read<AtmModel.AtmItem>(response);
            return atm.Id;
        }

        public async Task<Guid> CreateConfiguredAtm(CassetteSpec[] cassettes)
        {
            var atmId = await CreateAtm();
            await ApiClient.EnsureSuccess(await _api.Put($"/api/atms/{atmId}/cassettes", new { cassettes }));
            return atmId;
        }

        public async Task<Guid> CreateInServiceAtm(CassetteSpec[] cassettes, params LoadItem[] loads)
        {
            var atmId = await CreateConfiguredAtm(cassettes);
            await ApiClient.EnsureSuccess(await _api.Post($"/api/atms/{atmId}/load", new { items = loads }, (UserHeader, "operator")));
            await ApiClient.EnsureSuccess(await _api.Post($"/api/atms/{atmId}/put-in-service"));
            return atmId;
        }

        public async Task<AtmModel.GetDetail.Return> GetDetail(Guid atmId)
        {
            return await ApiClient.Read<AtmModel.GetDetail.Return>(await _api.Get($"/api/atms/{atmId}"));
        }

        public async Task<AtmModel.CassetteItem> GetCassette(Guid atmId, int position)
        {
            var detail = await GetDetail(atmId);
            return detail.Cassettes.Single(cassette => cassette.Position == position);
        }

        public Task<HttpResponseMessage> RequestQuote(Guid atmId, string cardNumber, Currency currency, int amount)
        {
            return _api.Post($"/api/atms/{atmId}/withdrawals/quotes", new { cardNumber, currency, amount });
        }

        public async Task<WithdrawalModel.CreateQuote.Return> CreateQuote(Guid atmId, string cardNumber, Currency currency, int amount)
        {
            var response = await RequestQuote(atmId, cardNumber, currency, amount);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            return await ApiClient.Read<WithdrawalModel.CreateQuote.Return>(response);
        }

        public Task<HttpResponseMessage> ConfirmQuote(Guid atmId, Guid quoteId, string idempotencyKey, QuoteOptionType? option = null)
        {
            var body = option.HasValue ? new { option = option.Value } : null;
            return _api.Post($"/api/atms/{atmId}/withdrawals/quotes/{quoteId}/confirm", body, (IdempotencyHeader, idempotencyKey));
        }

        public async Task AssertConsistent(Guid atmId)
        {
            var consistency = await ApiClient.Read<ConsistencyModel.Check.Return>(await _api.Get($"/api/atms/{atmId}/consistency"));
            Assert.True(consistency.IsConsistent, $"Ledger and stored balances differ: {consistency.DifferenceCount} difference(s).");
        }
    }
}
