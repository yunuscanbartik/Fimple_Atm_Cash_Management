using Fimple.Atm.Domain.Entities;
using Fimple.Atm.Domain.Enums;

namespace Fimple.Atm.Domain.Interfaces
{
    public interface ICassetteMovementRecorder
    {
        public CassetteContent GetOrAddContent(Cassette cassette, Currency currency, Denomination denomination);
        public void Record(CassetteMovementRequest movementRequest);
    }

    public class CassetteMovementRequest
    {
        public required Cassette Cassette { get; init; }
        public required CassetteContent Content { get; init; }
        public required CassetteMovementType MovementType { get; init; }
        public int NoteCountDelta { get; init; }
        public int ReservedCountDelta { get; init; }
        public Guid? ReferenceId { get; init; }
        public string? PerformedBy { get; init; }
    }
}
