using Fimple.Atm.Domain.Constants;
using Fimple.Atm.Domain.Enums;
using Fimple.Atm.Domain.Exceptions;
using Fimple.Atm.Domain.Interfaces;
using Fimple.Atm.Domain.Models;

namespace Fimple.Atm.Application.Services
{
    public class ConsistencyService : IConsistencyService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAtmRepository _atmRepository;
        private readonly ICassetteMovementRepository _movementRepository;
        private readonly TimeProvider _timeProvider;

        public ConsistencyService(
            IUnitOfWork unitOfWork,
            IAtmRepository atmRepository,
            ICassetteMovementRepository movementRepository,
            TimeProvider timeProvider)
        {
            _unitOfWork = unitOfWork;
            _atmRepository = atmRepository;
            _movementRepository = movementRepository;
            _timeProvider = timeProvider;
        }

        public async Task<ConsistencyModel.Check.Return> CheckConsistency(ConsistencyModel.Check.Request request)
        {
            await using var transaction = await _unitOfWork.BeginRepeatableReadTransaction();

            var atmExists = await _atmRepository.Exists(request.AtmId);
            if (!atmExists)
            {
                throw new NotFoundException(ErrorCodes.ATM_NOT_FOUND, $"ATM not found: {request.AtmId}");
            }

            var cassettes = await _atmRepository.GetCassettesWithContentsReadOnly(request.AtmId);
            var cassetteIds = cassettes.Select(cassette => cassette.Id).ToList();

            var movementSums = await _movementRepository.GetSums(cassetteIds);

            var entries = BuildEntries(cassettes, movementSums);
            var differences = entries.Where(entry => !entry.IsConsistent).ToList();

            var checkedAt = _timeProvider.GetUtcNow().UtcDateTime;
            var isConsistent = differences.Count == 0;
            var differenceCount = differences.Count;

            return new ConsistencyModel.Check.Return
            {
                AtmId = request.AtmId,
                CheckedAt = checkedAt,
                IsConsistent = isConsistent,
                DifferenceCount = differenceCount,
                Differences = differences,
                Entries = entries
            };
        }

        private static List<ConsistencyModel.EntryItem> BuildEntries(
            List<Domain.Entities.Cassette> cassettes,
            List<CassetteMovementSum> movementSums)
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
        }    }
}
