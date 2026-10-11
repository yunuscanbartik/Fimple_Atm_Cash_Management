using Fimple.Atm.Domain.Entities;
using Fimple.Atm.Domain.Enums;
using Fimple.Atm.Domain.Interfaces;
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

            content.NoteCount = noteCount;
            content.ReservedCount = reservedCount;

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
                CreatedAt = _timeProvider.GetUtcNow().UtcDateTime
            };
            _appDbContext.CassetteMovements.Add(movement);
        }
    }
}
