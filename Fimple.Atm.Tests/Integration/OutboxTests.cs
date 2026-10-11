using Fimple.Atm.Domain.Enums;
using Fimple.Atm.Domain.Interfaces;
using Fimple.Atm.Infrastructure.PostgreSQL;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Fimple.Atm.Tests.Integration
{
    [Collection(ApiCollection.NAME)]
    public class OutboxTests : IntegrationTestBase
    {
        public OutboxTests(ApiFixture fixture) : base(fixture)
        {

        }

        [Fact]
        public async Task DroppingBelowTenPercentCreatesOneEventThatTheProcessorMarksAsSent()
        {
            var atmId = await Atms.CreateInServiceAtm(AtmTestHelper.StandardCassettes, new LoadItem(4, 11));

            await Atms.CreateQuote(atmId, AtmTestHelper.NewCardNumber(), Currency.Try, 20);
            Assert.Equal(0, await CountOutboxEvents(atmId));

            await Atms.CreateQuote(atmId, AtmTestHelper.NewCardNumber(), Currency.Try, 20);
            Assert.Equal(1, await CountOutboxEvents(atmId));

            await Atms.CreateQuote(atmId, AtmTestHelper.NewCardNumber(), Currency.Try, 20);
            Assert.Equal(1, await CountOutboxEvents(atmId));

            var processedCount = await Fixture.WithScopedService<IOutboxProcessor, int>(processor => processor.ProcessPendingEvents());
            Assert.True(processedCount >= 1);
            Assert.Equal(0, await CountOutboxEvents(atmId, OutboxEventStatus.Pending));
            Assert.Equal(1, await CountOutboxEvents(atmId, OutboxEventStatus.Sent));
        }

        [Fact]
        public async Task TheEventIsRolledBackTogetherWithTheOperationThatTriggeredIt()
        {
            var atmId = await Atms.CreateInServiceAtm(AtmTestHelper.StandardCassettes, new LoadItem(4, 11));

            using (var scope = Fixture.Services.CreateScope())
            {
                var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var recorder = scope.ServiceProvider.GetRequiredService<ICassetteMovementRecorder>();

                await using var transaction = await appDbContext.Database.BeginTransactionAsync();
                var atm = await appDbContext.LoadAtmForUpdate(atmId);
                var cassette = atm.Cassettes.Single(atmCassette => atmCassette.Position == 4);
                var content = cassette.Contents.Single();
                recorder.Record(new CassetteMovementRequest
                {
                    Cassette = cassette,
                    Content = content,
                    MovementType = CassetteMovementType.Unload,
                    NoteCountDelta = -5
                });
                await appDbContext.SaveChangesAsync();

                var visibleInsideTransaction = await appDbContext.Database
                    .SqlQuery<int>($"SELECT count(*)::int AS \"Value\" FROM \"t_OutboxEvents\" WHERE \"Payload\"->>'atmId' = {atmId.ToString()}")
                    .SingleAsync();
                Assert.Equal(1, visibleInsideTransaction);

                await transaction.RollbackAsync();
            }

            Assert.Equal(0, await CountOutboxEvents(atmId));
            Assert.Equal(11, (await Atms.GetCassette(atmId, 4)).NoteCount);
        }

        private Task<int> CountOutboxEvents(Guid atmId, OutboxEventStatus? status = null)
        {
            var statusFilter = status?.ToString();
            return Fixture.WithDbContext(appDbContext => appDbContext.Database
                .SqlQuery<int>($"SELECT count(*)::int AS \"Value\" FROM \"t_OutboxEvents\" WHERE \"Payload\"->>'atmId' = {atmId.ToString()} AND ({statusFilter}::text IS NULL OR \"Status\" = {statusFilter}::text)")
                .SingleAsync());
        }
    }
}
