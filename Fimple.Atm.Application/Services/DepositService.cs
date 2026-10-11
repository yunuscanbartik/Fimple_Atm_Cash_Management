using Fimple.Atm.Domain.Constants;
using Fimple.Atm.Domain.Entities;
using Fimple.Atm.Domain.Enums;
using Fimple.Atm.Domain.Exceptions;
using Fimple.Atm.Domain.Interfaces;
using Fimple.Atm.Domain.Models;
using Fimple.Atm.Infrastructure.PostgreSQL;

namespace Fimple.Atm.Application.Services
{
    public class DepositService : IDepositService
    {
        private readonly AppDbContext _appDbContext;
        private readonly ICassetteMovementRecorder _cassetteMovementRecorder;
        private readonly TimeProvider _timeProvider;

        public DepositService(AppDbContext appDbContext, ICassetteMovementRecorder cassetteMovementRecorder, TimeProvider timeProvider)
        {
            _appDbContext = appDbContext;
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

            await using var transaction = await _appDbContext.Database.BeginTransactionAsync();
            var atm = await _appDbContext.LoadAtmForUpdate(request.AtmId);
            if (atm.Status != AtmStatus.InService)
            {
                throw new ConflictException(ErrorCodes.INVALID_ATM_STATE,
                    $"Deposits are only accepted while the ATM is {AtmStatus.InService}; it is {atm.Status}.");
            }

            var allocation = DepositAllocator.Allocate(BuildCassetteSpaces(atm), depositLines);
            var deposit = BuildDeposit(atm, request.UnrecognisedNoteCount, allocation);
            _appDbContext.DepositTransactions.Add(deposit);
            RecordPlacements(atm, deposit.Id, allocation);

            await _appDbContext.SaveChangesAsync();
            await transaction.CommitAsync();

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
            var deposit = new DepositTransaction
            {
                Id = Guid.NewGuid(),
                AtmId = atm.Id,
                UnrecognisedNoteCount = unrecognisedNoteCount,
                CreatedAt = _timeProvider.GetUtcNow().UtcDateTime
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
                .Select(placement => new DepositModel.AcceptedNoteItem
                {
                    Currency = placement.Currency,
                    Denomination = (int)placement.Denomination,
                    NoteCount = placement.NoteCount,
                    CassetteId = placement.CassetteId,
                    CassettePosition = cassettesById[placement.CassetteId].Position,
                    CassetteKind = cassettesById[placement.CassetteId].Kind
                })
                .OrderBy(note => note.CassettePosition)
                .ThenBy(note => note.Currency)
                .ThenBy(note => note.Denomination)
                .ToList();

            var returnedNotes = allocation.Returns
                .Select(returned => new DepositModel.ReturnedNoteItem
                {
                    Currency = returned.Currency,
                    Denomination = (int)returned.Denomination,
                    NoteCount = returned.NoteCount
                })
                .ToList();

            var acceptedTotals = allocation.Placements
                .GroupBy(placement => placement.Currency)
                .Select(group => new DepositModel.AcceptedTotalItem
                {
                    Currency = group.Key,
                    Amount = group.Sum(placement => (decimal)placement.NoteCount * (int)placement.Denomination)
                })
                .OrderBy(total => total.Currency)
                .ToList();

            return new DepositModel.Add.Return
            {
                DepositId = deposit.Id,
                AtmId = atm.Id,
                AcceptedNotes = acceptedNotes,
                ReturnedNotes = returnedNotes,
                AcceptedTotals = acceptedTotals,
                UnrecognisedNoteCount = deposit.UnrecognisedNoteCount,
                AcceptedNoteCount = acceptedNotes.Sum(note => note.NoteCount),
                ReturnedNoteCount = returnedNotes.Sum(note => note.NoteCount)
            };
        }
    }
}
