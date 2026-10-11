using Fimple.Atm.Domain.Enums;

namespace Fimple.Atm.Application.Services
{
    public record NoteStock(Guid CassetteId, int Position, Denomination Denomination, int AvailableCount, int Capacity);

    public record NoteAllocation(Guid CassetteId, Denomination Denomination, int NoteCount);

    public class NoteBreakdown
    {
        public required int Amount { get; init; }
        public required IReadOnlyList<NoteAllocation> Allocations { get; init; }
        public int NoteCount => Allocations.Sum(allocation => allocation.NoteCount);
    }

    public class NoteSelectionResult
    {
        public NoteBreakdown? Exact { get; init; }
        public NoteBreakdown? Lower { get; init; }
        public NoteBreakdown? Upper { get; init; }
    }
}
