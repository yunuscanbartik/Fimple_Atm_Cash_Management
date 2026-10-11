using System.Net;
using Fimple.Atm.Domain.Enums;
using Fimple.Atm.Domain.Models;

namespace Fimple.Atm.Tests.Integration
{
    [Collection(ApiCollection.Name)]
    public class EndToEndScenarioTests : IntegrationTestBase
    {
        public EndToEndScenarioTests(ApiFixture fixture) : base(fixture)
        {

        }

        [Fact]
        public async Task CreateConfigureLoadServeWithdrawDepositReconcileAndReport()
        {
            var atmId = await Atms.CreateAtm();
            var created = await ApiClient.Read<AtmModel.AtmDetail>(await Api.Get($"/api/atms/{atmId}"));
            Assert.Equal(AtmStatus.Unconfigured, created.Status);

            var configured = await ApiClient.Read<AtmModel.AtmDetail>(
                await Api.Put($"/api/atms/{atmId}/cassettes", new { cassettes = AtmTestHelper.StandardCassettes }));
            Assert.Equal(AtmStatus.Configured, configured.Status);
            Assert.Equal(5, configured.Cassettes.Count);

            var loadItems = new[]
            {
                new LoadItem(2, 20), new LoadItem(3, 30), new LoadItem(4, 50), new LoadItem(5, 10)
            };
            var loaded = await ApiClient.Read<AtmModel.AtmDetail>(await Api.Post($"/api/atms/{atmId}/load", new { items = loadItems }));
            Assert.Equal(AtmStatus.Loaded, loaded.Status);

            var inService = await ApiClient.Read<AtmModel.AtmItem>(await Api.Post($"/api/atms/{atmId}/put-in-service"));
            Assert.Equal(AtmStatus.InService, inService.Status);

            var quote = await Atms.CreateQuote(atmId, AtmTestHelper.NewCardNumber(), Currency.Try, 170);
            Assert.True(quote.IsExactMatch);
            var confirm = await ApiClient.Read<WithdrawalModel.ConfirmItem>(await Atms.ConfirmQuote(atmId, quote.QuoteId, "e2e-key"));
            Assert.Equal(QuoteStatus.Confirmed, confirm.Status);
            Assert.Equal(170m, confirm.Amount);

            var deposit = await ApiClient.Read<DepositModel.Add.Return>(await Api.Post($"/api/atms/{atmId}/deposits", new
            {
                notes = new object[]
                {
                    new { currency = Currency.Try, denomination = 100, noteCount = 2 },
                    new { currency = Currency.Eur, denomination = 50, noteCount = 1 }
                },
                unrecognisedNoteCount = 1
            }));
            Assert.Equal(3, deposit.AcceptedNoteCount);
            Assert.Equal(1, deposit.UnrecognisedNoteCount);

            var maintenance = await ApiClient.Read<AtmModel.AtmItem>(await Api.Post($"/api/atms/{atmId}/enter-maintenance"));
            Assert.Equal(AtmStatus.Maintenance, maintenance.Status);

            var reconciliation = await ApiClient.Read<ReconciliationModel.ReconciliationItem>(await Api.Post(
                $"/api/atms/{atmId}/reconciliations",
                new
                {
                    counts = new object[]
                    {
                        new { cassettePosition = 2, countedNoteCount = 21 },
                        new { cassettePosition = 3, countedNoteCount = 29 },
                        new { cassettePosition = 4, countedNoteCount = 49 },
                        new { cassettePosition = 5, countedNoteCount = 10 },
                        new { cassettePosition = 1, currency = Currency.Eur, denomination = 50, countedNoteCount = 1 }
                    }
                },
                (AtmTestHelper.UserHeader, "alice")));
            Assert.Equal(ReconciliationStatus.Completed, reconciliation.Status);
            Assert.Equal(0m, reconciliation.TotalDifferenceInBaseCurrency);

            var report = await ApiClient.Read<ReportModel.Operations.Return>(await Api.Get($"/api/reports/operations?atmId={atmId}"));
            var atmReport = Assert.Single(report.Atms);
            Assert.Equal(21 * 100 + 29 * 50 + 49 * 20, atmReport.Totals.Single(total => total.Currency == Currency.Try).Amount);
            Assert.Equal(48980m, report.TotalInBaseCurrency);

            await Atms.AssertConsistent(atmId);

            var backInService = await Api.Post($"/api/atms/{atmId}/put-in-service");
            Assert.Equal(HttpStatusCode.OK, backInService.StatusCode);
        }
    }
}
