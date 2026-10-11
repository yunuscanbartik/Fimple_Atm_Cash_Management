using System.Text.Json;
using Fimple.Atm.Domain.Entities;
using Fimple.Atm.Domain.Enums;
using Fimple.Atm.Domain.Interfaces;
using Fimple.Atm.Domain.Models;
using Fimple.Atm.Infrastructure.PostgreSQL;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Fimple.Atm.Application.Services
{
    public class OutboxProcessor : IOutboxProcessor
    {
        private const int BATCH_SIZE = 50;

        private readonly AppDbContext _appDbContext;
        private readonly TimeProvider _timeProvider;
        private readonly ILogger<OutboxProcessor> _logger;

        public OutboxProcessor(AppDbContext appDbContext, TimeProvider timeProvider, ILogger<OutboxProcessor> logger)
        {
            _appDbContext = appDbContext;
            _timeProvider = timeProvider;
            _logger = logger;
        }

        public async Task<int> ProcessPendingEvents()
        {
            await using var transaction = await _appDbContext.Database.BeginTransactionAsync();

            var pendingEvents = await _appDbContext.OutboxEvents
                .FromSqlInterpolated($"SELECT * FROM \"t_OutboxEvents\" WHERE \"Status\" = {nameof(OutboxEventStatus.Pending)} ORDER BY \"CreatedAt\" LIMIT {BATCH_SIZE} FOR UPDATE SKIP LOCKED")
                .ToListAsync();

            var sentAt = _timeProvider.GetUtcNow().UtcDateTime;
            foreach (var outboxEvent in pendingEvents)
            {
                LogEvent(outboxEvent);
                outboxEvent.Status = OutboxEventStatus.Sent;
                outboxEvent.SentAt = sentAt;
            }

            await _appDbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            _appDbContext.ChangeTracker.Clear();
            return pendingEvents.Count;
        }

        private void LogEvent(OutboxEvent outboxEvent)
        {
            if (outboxEvent.EventType != OutboxEventType.LowCashAlert)
            {
                _logger.LogWarning("Outbox event {EventId} has an unsupported type {EventType} and is marked as sent without handling.",
                    outboxEvent.Id, outboxEvent.EventType);
                return;
            }

            var payload = JsonSerializer.Deserialize<LowCashAlertPayload>(outboxEvent.Payload, OutboxJson.Options);
            if (payload == null)
            {
                _logger.LogWarning("Outbox event {EventId} has an empty payload and is marked as sent without handling.", outboxEvent.Id);
                return;
            }

            _logger.LogWarning(
                "LOW CASH ALERT: ATM {AtmId} cassette {CassettePosition} ({Currency} {Denomination}) has {AvailableCount} of {Capacity} notes available (triggered by {TriggeredBy}).",
                payload.AtmId, payload.CassettePosition, payload.Currency, payload.Denomination,
                payload.AvailableCount, payload.Capacity, payload.TriggeredBy);
        }
    }
}
