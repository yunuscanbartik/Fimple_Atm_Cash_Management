using Fimple.Atm.Domain.Constants;
using Fimple.Atm.Domain.Entities;
using Fimple.Atm.Domain.Enums;
using Fimple.Atm.Domain.Exceptions;
using Fimple.Atm.Domain.Interfaces;
using Fimple.Atm.Domain.Models;

namespace Fimple.Atm.Application.Services
{
    public class DepositService : IDepositService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAtmRepository _atmRepository;
        private readonly IDepositRepository _depositRepository;
        private readonly ICassetteMovementRecorder _cassetteMovementRecorder;
        private readonly TimeProvider _timeProvider;

        public DepositService(
            IUnitOfWork unitOfWork,
            IAtmRepository atmRepository,
            IDepositRepository depositRepository,
            ICassetteMovementRecorder cassetteMovementRecorder,
            TimeProvider timeProvider)
        {
            _unitOfWork = unitOfWork;
            _atmRepository = atmRepository;
            _depositRepository = depositRepository;
            _cassetteMovementRecorder = cassetteMovementRecorder;
            _timeProvider = timeProvider;
        }

        public async Task<DepositModel.Add.Return> AddDeposit(DepositModel.Add.Request request)
        {
            var depositLines = MergeLines(request.Notes);
            if (depositLines.Count == 0 && request.UnrecognisedNoteCount == 0)
            {
                throw new BusinessRuleException(ErrorCodes.EMPTY_DEPOSIT, "A deposit must contain at least one note.");
            }

            await using var transaction = await _unitOfWork.BeginTransaction();
            var atm = await _atmRepository.LoadForUpdate(request.AtmId);
            if (atm.Status != AtmStatus.InService)
            {
                throw new ConflictException(ErrorCodes.INVALID_ATM_STATE,
                    $"Deposits are only accepted while the ATM is {AtmStatus.InService}; it is {atm.Status}.");
            }

            var allocation = DepositAllocator.Allocate(BuildCassetteSpaces(atm), depositLines);
            var deposit = BuildDeposit(atm, request.UnrecognisedNoteCount, allocation);
            _depositRepository.Add(deposit);
            RecordPlacements(atm, deposit.Id, allocation);

            await _unitOfWork.SaveChanges();
            await transaction.Commit();

            return BuildResponse(atm, deposit, allocation);
        }

        private static List<DepositLine> MergeLines(IEnumerable<DepositModel.DepositNoteItem> notes)
        {
            return notes
                .GroupBy(note => (note.Currency, Denomination: (Denomination)note.Denomination))
                .Select(group => new DepositLine(group.Key.Currency, group.Key.Denomination, group.Sum(note => note.NoteCount)))
                .ToList();
        }

        private static List<CassetteSpace> BuildCassetteSpaces(AtmMachine atm)
        {
            return atm.Cassettes
                .Select(cassette => new CassetteSpace(
                    cassette.Id,
                    cassette.Position,
                    cassette.Kind,
                    cassette.Currency,
                    cassette.Denomination,
                    cassette.Capacity,
                    cassette.Contents.Sum(content => content.NoteCount)))
                .ToList();
        }

        private DepositTransaction BuildDeposit(AtmMachine atm, int unrecognisedNoteCount, DepositAllocation allocation)
        {
            var createdAt = _timeProvider.GetUtcNow().UtcDateTime;
            var deposit = new DepositTransaction
            {
                Id = Guid.NewGuid(),
                AtmId = atm.Id,
                UnrecognisedNoteCount = unrecognisedNoteCount,
                CreatedAt = createdAt
            };

            foreach (var placement in allocation.Placements)
            {
                deposit.Notes.Add(new DepositNote
                {
                    Id = Guid.NewGuid(),
                    DepositTransactionId = deposit.Id,
                    Currency = placement.Currency,
                    Denomination = placement.Denomination,
                    NoteCount = placement.NoteCount,
                    CassetteId = placement.CassetteId
                });
            }

            foreach (var returned in allocation.Returns)
            {
                deposit.Notes.Add(new DepositNote
                {
                    Id = Guid.NewGuid(),
                    DepositTransactionId = deposit.Id,
                    Currency = returned.Currency,
                    Denomination = returned.Denomination,
                    NoteCount = returned.NoteCount,
                    CassetteId = null
                });
            }

            return deposit;
        }

        private void RecordPlacements(AtmMachine atm, Guid depositId, DepositAllocation allocation)
        {
            foreach (var placement in allocation.Placements)
            {
                var cassette = atm.Cassettes.FirstOrDefault(atmCassette => atmCassette.Id == placement.CassetteId);
                if (cassette == null)
                {
                    throw new InvalidOperationException($"Cassette {placement.CassetteId} does not belong to ATM {atm.Id}.");
                }

                var content = _cassetteMovementRecorder.GetOrAddContent(cassette, placement.Currency, placement.Denomination);
                _cassetteMovementRecorder.Record(new CassetteMovementRequest
                {
                    Cassette = cassette,
                    Content = content,
                    MovementType = CassetteMovementType.Deposit,
                    NoteCountDelta = placement.NoteCount,
                    ReferenceId = depositId
                });
            }
        }

        private static DepositModel.Add.Return BuildResponse(AtmMachine atm, DepositTransaction deposit, DepositAllocation allocation)
        {
            var cassettesById = atm.Cassettes.ToDictionary(cassette => cassette.Id);

            var acceptedNotes = allocation.Placements
                .Select(placement => ToAcceptedNoteItem(placement, cassettesById))
                .OrderBy(note => note.CassettePosition)
                .ThenBy(note => note.Currency)
                .ThenBy(note => note.Denomination)
                .ToList();

            var returnedNotes = allocation.Returns
                .Select(ToReturnedNoteItem)
                .ToList();

            var acceptedTotals = allocation.Placements
                .GroupBy(placement => placement.Currency)
                .Select(ToAcceptedTotalItem)
                .OrderBy(total => total.Currency)
                .ToList();

            var acceptedNoteCount = acceptedNotes.Sum(note => note.NoteCount);
            var returnedNoteCount = returnedNotes.Sum(note => note.NoteCount);

            return new DepositModel.Add.Return
            {
                DepositId = deposit.Id,
                AtmId = atm.Id,
                AcceptedNotes = acceptedNotes,
                ReturnedNotes = returnedNotes,
                AcceptedTotals = acceptedTotals,
                UnrecognisedNoteCount = deposit.UnrecognisedNoteCount,
                AcceptedNoteCount = acceptedNoteCount,
                ReturnedNoteCount = returnedNoteCount
            };
        }

        private static DepositModel.AcceptedNoteItem ToAcceptedNoteItem(DepositPlacement placement, Dictionary<Guid, Cassette> cassettesById)
        {
            var cassette = cassettesById[placement.CassetteId];
            var denomination = (int)placement.Denomination;

            return new DepositModel.AcceptedNoteItem
            {
                Currency = placement.Currency,
                Denomination = denomination,
                NoteCount = placement.NoteCount,
                CassetteId = placement.CassetteId,
                CassettePosition = cassette.Position,
                CassetteKind = cassette.Kind
            };
        }

        private static DepositModel.ReturnedNoteItem ToReturnedNoteItem(DepositReturn returned)
        {
            var denomination = (int)returned.Denomination;

            return new DepositModel.ReturnedNoteItem
            {
                Currency = returned.Currency,
                Denomination = denomination,
                NoteCount = returned.NoteCount
            };
        }

        private static DepositModel.AcceptedTotalItem ToAcceptedTotalItem(IGrouping<Currency, DepositPlacement> group)
        {
            var amount = group.Sum(placement => (decimal)placement.NoteCount * (int)placement.Denomination);

            return new DepositModel.AcceptedTotalItem
            {
                Currency = group.Key,
                Amount = amount
            };
        }
    }
}
