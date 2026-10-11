using System.Net;
using Fimple.Atm.Domain.Enums;
using Fimple.Atm.Domain.Models;

namespace Fimple.Atm.Tests.Integration
{
    [Collection(ApiCollection.Name)]
    public class DepositAndReconciliationTests : IntegrationTestBase
    {
        public DepositAndReconciliationTests(ApiFixture fixture) : base(fixture)
        {

        }

        [Fact]
        public async Task DepositOverflowsToTheDepositCassetteAndReturnsWhatFitsNowhere()
        {
            var atmId = await Atms.CreateInServiceAtm(AtmTestHelper.StandardCassettes, new LoadItem(2, 95), new LoadItem(1, 93, Currency.Eur, 50));

            var response = await Api.Post($"/api/atms/{atmId}/deposits", new
            {
                notes = new object[]
                {
                    new { currency = Currency.Try, denomination = 100, noteCount = 12 },
                    new { currency = Currency.Usd, denomination = 20, noteCount = 1 }
                },
                unrecognisedNoteCount = 2
            });
            var deposit = await ApiClient.Read<DepositModel.Add.Return>(response);

            var toRecycling = deposit.AcceptedNotes.Single(note => note.CassetteKind == CassetteKind.Recycling);
            var toDepositCassette = deposit.AcceptedNotes.Single(note => note.CassetteKind == CassetteKind.Deposit);
            Assert.Equal(5, toRecycling.NoteCount);
            Assert.Equal(7, toDepositCassette.NoteCount);
            var returned = Assert.Single(deposit.ReturnedNotes);
            Assert.Equal(Currency.Usd, returned.Currency);
            Assert.Equal(2, deposit.UnrecognisedNoteCount);
            Assert.Equal(100, (await Atms.GetCassette(atmId, 1)).NoteCount);
            await Atms.AssertConsistent(atmId);
        }

        [Fact]
        public async Task SmallReconciliationDifferenceCompletesAndAdjustsTheBalance()
        {
            var atmId = await CreateAtmInMaintenance();

            var reconciliation = await Reconcile(atmId, "alice", pos2: 9, pos3: 10, pos4: 10, pos5: 10);

            Assert.Equal(ReconciliationStatus.Completed, reconciliation.Status);
            Assert.Equal(100m, reconciliation.TotalDifferenceInBaseCurrency);
            Assert.Equal(9, (await Atms.GetCassette(atmId, 2)).NoteCount);
            await Atms.AssertConsistent(atmId);
            await ApiClient.EnsureSuccess(await Api.Post($"/api/atms/{atmId}/put-in-service"));
        }

        [Fact]
        public async Task LargeReconciliationDifferenceNeedsAnotherUserBeforeTheAtmReturnsToService()
        {
            var atmId = await CreateAtmInMaintenance();

            var reconciliation = await Reconcile(atmId, "alice", pos2: 10, pos3: 10, pos4: 10, pos5: 8);

            Assert.Equal(ReconciliationStatus.AwaitingApproval, reconciliation.Status);
            Assert.Equal(8400m, reconciliation.TotalDifferenceInBaseCurrency);

            var blocked = await Api.Post($"/api/atms/{atmId}/put-in-service");
            Assert.Equal(HttpStatusCode.Conflict, blocked.StatusCode);
            Assert.Equal("RECONCILIATION_AWAITING_APPROVAL", await ApiClient.ErrorCode(blocked));

            var selfApproval = await Api.Post($"/api/atms/{atmId}/reconciliations/{reconciliation.Id}/approve", null, (AtmTestHelper.UserHeader, "ALICE"));
            Assert.Equal(HttpStatusCode.Forbidden, selfApproval.StatusCode);
            Assert.Equal("FOUR_EYES_VIOLATION", await ApiClient.ErrorCode(selfApproval));

            var noUser = await Api.Post($"/api/atms/{atmId}/reconciliations/{reconciliation.Id}/approve");
            Assert.Equal(HttpStatusCode.BadRequest, noUser.StatusCode);

            var approved = await ApiClient.Read<ReconciliationModel.ReconciliationItem>(
                await Api.Post($"/api/atms/{atmId}/reconciliations/{reconciliation.Id}/approve", null, (AtmTestHelper.UserHeader, "bob")));
            Assert.Equal(ReconciliationStatus.Approved, approved.Status);
            Assert.Equal("bob", approved.ApprovedBy);

            var inService = await Api.Post($"/api/atms/{atmId}/put-in-service");
            Assert.Equal(HttpStatusCode.OK, inService.StatusCode);
            await Atms.AssertConsistent(atmId);
        }

        [Fact]
        public async Task ReconciliationRequiresMaintenanceAUserAndEveryRecyclingCassette()
        {
            var inServiceAtmId = await Atms.CreateInServiceAtm(AtmTestHelper.StandardCassettes, new LoadItem(2, 5));
            var wrongState = await Api.Post($"/api/atms/{inServiceAtmId}/reconciliations",
                new { counts = new[] { new { cassettePosition = 2, countedNoteCount = 5 } } }, (AtmTestHelper.UserHeader, "alice"));
            Assert.Equal(HttpStatusCode.Conflict, wrongState.StatusCode);

            var atmId = await CreateAtmInMaintenance();
            var noUser = await Api.Post($"/api/atms/{atmId}/reconciliations",
                new { counts = new[] { new { cassettePosition = 2, countedNoteCount = 5 } } });
            Assert.Equal("USER_HEADER_REQUIRED", await ApiClient.ErrorCode(noUser));

            var incomplete = await Api.Post($"/api/atms/{atmId}/reconciliations",
                new { counts = new[] { new { cassettePosition = 2, countedNoteCount = 5 } } }, (AtmTestHelper.UserHeader, "alice"));
            Assert.Equal(HttpStatusCode.UnprocessableEntity, incomplete.StatusCode);
            Assert.Equal("INVALID_RECONCILIATION_COUNTS", await ApiClient.ErrorCode(incomplete));
        }

        [Fact]
        public async Task ConsistencyCheckReportsADifferenceWhenTheStoredBalanceIsTamperedWith()
        {
            var atmId = await Atms.CreateInServiceAtm(AtmTestHelper.StandardCassettes, new LoadItem(2, 5));
            await Atms.AssertConsistent(atmId);

            await Fixture.WithDbContext(async appDbContext =>
            {
                var content = appDbContext.CassetteContents.Single(cassetteContent =>
                    cassetteContent.Cassette.AtmId == atmId && cassetteContent.Cassette.Position == 2);
                content.NoteCount += 3;
                return await appDbContext.SaveChangesAsync();
            });

            var consistency = await ApiClient.Read<ConsistencyModel.Check.Return>(await Api.Get($"/api/atms/{atmId}/consistency"));

            Assert.False(consistency.IsConsistent);
            var difference = Assert.Single(consistency.Differences);
            Assert.Equal(8, difference.StoredNoteCount);
            Assert.Equal(5, difference.CalculatedNoteCount);
        }

        private async Task<Guid> CreateAtmInMaintenance()
        {
            var atmId = await Atms.CreateInServiceAtm(AtmTestHelper.StandardCassettes,
                new LoadItem(2, 10), new LoadItem(3, 10), new LoadItem(4, 10), new LoadItem(5, 10));
            await ApiClient.EnsureSuccess(await Api.Post($"/api/atms/{atmId}/enter-maintenance"));
            return atmId;
        }

        private async Task<ReconciliationModel.ReconciliationItem> Reconcile(Guid atmId, string userName, int pos2, int pos3, int pos4, int pos5)
        {
            var response = await Api.Post($"/api/atms/{atmId}/reconciliations", new
            {
                counts = new[]
                {
                    new { cassettePosition = 2, countedNoteCount = pos2 },
                    new { cassettePosition = 3, countedNoteCount = pos3 },
                    new { cassettePosition = 4, countedNoteCount = pos4 },
                    new { cassettePosition = 5, countedNoteCount = pos5 }
                }
            }, (AtmTestHelper.UserHeader, userName));
            return await ApiClient.Read<ReconciliationModel.ReconciliationItem>(response);
        }
    }
}
