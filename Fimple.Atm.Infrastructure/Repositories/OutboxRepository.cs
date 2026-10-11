using Fimple.Atm.Domain.Entities;
using Fimple.Atm.Domain.Enums;
using Fimple.Atm.Domain.Interfaces;
using Fimple.Atm.Infrastructure.PostgreSQL;
using Microsoft.EntityFrameworkCore;

namespace Fimple.Atm.Infrastructure.Repositories
{
    public class OutboxRepository : IOutboxRepository
    {
        private const string LOCK_PENDING_EVENTS_SQL =
            $"SELECT * FROM \"{TableNames.OUTBOX_EVENTS}\" WHERE \"Status\" = {{0}} ORDER BY \"CreatedAt\" LIMIT {{1}} FOR UPDATE SKIP LOCKED";

        private readonly AppDbContext _appDbContext;

        public OutboxRepository(AppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }

        public async Task<List<OutboxEvent>> LockPendingEvents(int batchSize)
        {
            return await _appDbContext.OutboxEvents
                .FromSqlRaw(LOCK_PENDING_EVENTS_SQL, nameof(OutboxEventStatus.Pending), batchSize)
                .ToListAsync();
        }

        public void Add(OutboxEvent outboxEvent)
        {
            _appDbContext.OutboxEvents.Add(outboxEvent);
        }
    }
}
