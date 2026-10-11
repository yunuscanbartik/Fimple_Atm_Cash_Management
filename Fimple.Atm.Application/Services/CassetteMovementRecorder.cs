using System.Text.Json;
using Fimple.Atm.Domain.Constants;
using Fimple.Atm.Domain.Entities;
using Fimple.Atm.Domain.Enums;
using Fimple.Atm.Domain.Interfaces;
using Fimple.Atm.Domain.Models;
using Fimple.Atm.Infrastructure.PostgreSQL;

namespace Fimple.Atm.Application.Services
{
    public class CassetteMovementRecorder : ICassetteMovementRecorder
    {
        private readonly AppDbContext _appDbContext;
        private readonly TimeProvider _timeProvider;

        public CassetteMovementRecorder(AppDbContext appDbContext, TimeProvider timeProvider)
        {
            _appDbContext = appDbContext;
            _timeProvider = timeProvider;
        }

        public CassetteContent GetOrAddContent(Cassette cassette, Currency currency, Denomination denomination)
        {
            var content = cassette.Contents.FirstOrDefault(existingContent =>
                existingContent.Currency == currency && existingContent.Denomination == denomination);
            if (content != null) return content;

            content = new CassetteContent
            {
                Id = Guid.NewGuid(),
                CassetteId = cassette.Id,
                Currency = currency,
                Denomination = denomination
            };
            cassette.Contents.Add(content);
            return content;
        }

        public void Record(CassetteMovementRequest movementRequest)
        {
            var content = movementRequest.Content;
            var noteCount = content.NoteCount + movementRequest.NoteCountDelta;
            var reservedCount = content.ReservedCount + movementRequest.ReservedCountDelta;
            var isBalanceValid = noteCount >= 0 && reservedCount >= 0 && reservedCount <= noteCount;
            if (!isBalanceValid)
            {
                throw new InvalidOperationException(
                    $"Movement {movementRequest.MovementType} would leave cassette {movementRequest.Cassette.Id} with an invalid balance " +
                    $"(notes {noteCount}, reserved {reservedCount}).");
            }

            var availableCountBefore = content.AvailableCount;
            content.NoteCount = noteCount;
            content.ReservedCount = reservedCount;

            var now = _timeProvider.GetUtcNow().UtcDateTime;
            var movement = new CassetteMovement
            {
                Id = Guid.NewGuid(),
                CassetteId = movementRequest.Cassette.Id,
                MovementType = movementRequest.MovementType,
                Currency = content.Currency,
                Denomination = content.Denomination,
                NoteCountDelta = movementRequest.NoteCountDelta,
                ReservedCountDelta = movementRequest.ReservedCountDelta,
                ReferenceId = movementRequest.ReferenceId,
                PerformedBy = movementRequest.PerformedBy,
                CreatedAt = now
            };
            _appDbContext.CassetteMovements.Add(movement);

            AddLowCashAlertIfThresholdCrossed(movementRequest, availableCountBefore, now);
        }

        private void AddLowCashAlertIfThresholdCrossed(CassetteMovementRequest movementRequest, int availableCountBefore, DateTime now)
        {
            var cassette = movementRequest.Cassette;
            var content = movementRequest.Content;
            if (cassette.Kind != CassetteKind.Recycling) return;

            var wasLow = IsLowCash(availableCountBefore, cassette.Capacity);
            var isLow = IsLowCash(content.AvailableCount, cassette.Capacity);
            if (wasLow || !isLow) return;

            var payload = new LowCashAlertPayload
            {
                AtmId = cassette.AtmId,
                CassetteId = cassette.Id,
                CassettePosition = cassette.Position,
                Currency = content.Currency,
                Denomination = (int)content.Denomination,
                AvailableCount = content.AvailableCount,
                Capacity = cassette.Capacity,
                TriggeredBy = movementRequest.MovementType,
                ReferenceId = movementRequest.ReferenceId
            };

            _appDbContext.OutboxEvents.Add(new OutboxEvent
            {
                Id = Guid.NewGuid(),
                EventType = OutboxEventType.LowCashAlert,
                Payload = JsonSerializer.Serialize(payload, OutboxJson.Options),
                Status = OutboxEventStatus.Pending,
                CreatedAt = now
            });
        }

        private static bool IsLowCash(int availableCount, int capacity)
        {
            return (decimal)availableCount / capacity < AtmRules.LOW_CASH_THRESHOLD_RATIO;
        }
    }
}
