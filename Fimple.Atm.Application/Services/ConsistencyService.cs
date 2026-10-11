using System.Data;
using Fimple.Atm.Domain.Constants;
using Fimple.Atm.Domain.Enums;
using Fimple.Atm.Domain.Exceptions;
using Fimple.Atm.Domain.Interfaces;
using Fimple.Atm.Domain.Models;
using Fimple.Atm.Infrastructure.PostgreSQL;
using Microsoft.EntityFrameworkCore;

namespace Fimple.Atm.Application.Services
{
    public class ConsistencyService : IConsistencyService
    {
        private readonly AppDbContext _appDbContext;
        private readonly TimeProvider _timeProvider;

        public ConsistencyService(AppDbContext appDbContext, TimeProvider timeProvider)
        {
            _appDbContext = appDbContext;
            _timeProvider = timeProvider;
        }

        public async Task<ConsistencyModel.Check.Return> CheckConsistency(ConsistencyModel.Check.Request request)
        {
            await using var transaction = await _appDbContext.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead);

            var atmExists = await _appDbContext.AtmMachines.AnyAsync(atm => atm.Id == request.AtmId);
            if (!atmExists)
            {
                throw new NotFoundException(ErrorCodes.ATM_NOT_FOUND, $"ATM not found: {request.AtmId}");
            }

            var cassettes = await _appDbContext.Cassettes
                .AsNoTracking()
                .Include(cassette => cassette.Contents)
                .Where(cassette => cassette.AtmId == request.AtmId)
                .ToListAsync();
            var cassetteIds = cassettes.Select(cassette => cassette.Id).ToList();

            var movementSums = await _appDbContext.CassetteMovements
                .AsNoTracking()
                .Where(movement => cassetteIds.Contains(movement.CassetteId))
                .GroupBy(movement => new { movement.CassetteId, movement.Currency, movement.Denomination })
                .Select(group => new MovementSum(
                    group.Key.CassetteId,
                    group.Key.Currency,
                    group.Key.Denomination,
                    group.Sum(movement => movement.NoteCountDelta),
                    group.Sum(movement => movement.ReservedCountDelta)))
                .ToListAsync();

            var entries = BuildEntries(cassettes, movementSums);
            var differences = entries.Where(entry => !entry.IsConsistent).ToList();

            return new ConsistencyModel.Check.Return
            {
                AtmId = request.AtmId,
                CheckedAt = _timeProvider.GetUtcNow().UtcDateTime,
                IsConsistent = differences.Count == 0,
                DifferenceCount = differences.Count,
                Differences = differences,
                Entries = entries
            };
        }

        private static List<ConsistencyModel.EntryItem> BuildEntries(
            List<Domain.Entities.Cassette> cassettes,
            List<MovementSum> movementSums)
        {
            var entries = new List<ConsistencyModel.EntryItem>();

            foreach (var cassette in cassettes.OrderBy(existingCassette => existingCassette.Position))
            {
                var cassetteMovementSums = movementSums.Where(sum => sum.CassetteId == cassette.Id).ToList();
                var noteTypes = cassette.Contents
                    .Select(content => (content.Currency, content.Denomination))
                    .Union(cassetteMovementSums.Select(sum => (sum.Currency, sum.Denomination)))
                    .OrderBy(noteType => noteType.Currency)
                    .ThenBy(noteType => noteType.Denomination);

                foreach (var (currency, denomination) in noteTypes)
                {
                    var content = cassette.Contents.FirstOrDefault(existingContent =>
                        existingContent.Currency == currency && existingContent.Denomination == denomination);
                    var movementSum = cassetteMovementSums.FirstOrDefault(sum =>
                        sum.Currency == currency && sum.Denomination == denomination);

                    var storedNoteCount = content?.NoteCount ?? 0;
                    var storedReservedCount = content?.ReservedCount ?? 0;
                    var calculatedNoteCount = movementSum?.NoteCount ?? 0;
                    var calculatedReservedCount = movementSum?.ReservedCount ?? 0;
                    var isConsistent = storedNoteCount == calculatedNoteCount && storedReservedCount == calculatedReservedCount;

                    entries.Add(new ConsistencyModel.EntryItem
                    {
                        CassetteId = cassette.Id,
                        CassettePosition = cassette.Position,
                        Currency = currency,
                        Denomination = (int)denomination,
                        StoredNoteCount = storedNoteCount,
                        CalculatedNoteCount = calculatedNoteCount,
                        StoredReservedCount = storedReservedCount,
                        CalculatedReservedCount = calculatedReservedCount,
                        IsConsistent = isConsistent
                    });
                }
            }

            return entries;
        }

        private sealed record MovementSum(Guid CassetteId, Currency Currency, Denomination Denomination, int NoteCount, int ReservedCount);
    }
}
