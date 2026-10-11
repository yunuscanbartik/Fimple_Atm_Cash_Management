using Fimple.Atm.Domain.Enums;

namespace Fimple.Atm.Application.Services
{
    public record CassetteSpace(Guid CassetteId, int Position, CassetteKind Kind, Currency? Currency, Denomination? Denomination, int Capacity, int NoteCount);

    public record DepositLine(Currency Currency, Denomination Denomination, int NoteCount);

    public record DepositPlacement(Guid CassetteId, Currency Currency, Denomination Denomination, int NoteCount);

    public record DepositReturn(Currency Currency, Denomination Denomination, int NoteCount);

    public class DepositAllocation
    {
        public required IReadOnlyList<DepositPlacement> Placements { get; init; }
        public required IReadOnlyList<DepositReturn> Returns { get; init; }
    }

    public static class DepositAllocator
    {
        public static DepositAllocation Allocate(IReadOnlyCollection<CassetteSpace> cassetteSpaces, IReadOnlyList<DepositLine> depositLines)
        {
            var freeSpaces = cassetteSpaces.ToDictionary(space => space.CassetteId, space => space.Capacity - space.NoteCount);
            var depositCassette = cassetteSpaces.FirstOrDefault(space => space.Kind == CassetteKind.Deposit);
            var placements = new List<DepositPlacement>();
            var returns = new List<DepositReturn>();

            foreach (var line in depositLines)
            {
                var remaining = line.NoteCount;

                var matchingRecyclingCassettes = cassetteSpaces
                    .Where(space => space.Kind == CassetteKind.Recycling
                        && space.Currency == line.Currency
                        && space.Denomination == line.Denomination)
                    .OrderBy(space => (decimal)(space.Capacity - freeSpaces[space.CassetteId]) / space.Capacity)
                    .ThenBy(space => space.Position);

                foreach (var recyclingCassette in matchingRecyclingCassettes)
                {
                    remaining -= Place(recyclingCassette, line, remaining, freeSpaces, placements);
                }

                if (remaining > 0 && depositCassette != null)
                {
                    remaining -= Place(depositCassette, line, remaining, freeSpaces, placements);
                }

                if (remaining > 0)
                {
                    returns.Add(new DepositReturn(line.Currency, line.Denomination, remaining));
                }
            }

            return new DepositAllocation { Placements = placements, Returns = returns };
        }

        private static int Place(
            CassetteSpace cassetteSpace,
            DepositLine line,
            int remaining,
            Dictionary<Guid, int> freeSpaces,
            List<DepositPlacement> placements)
        {
            var placedCount = Math.Min(remaining, freeSpaces[cassetteSpace.CassetteId]);
            if (placedCount <= 0) return 0;

            freeSpaces[cassetteSpace.CassetteId] -= placedCount;
            placements.Add(new DepositPlacement(cassetteSpace.CassetteId, line.Currency, line.Denomination, placedCount));
            return placedCount;
        }
    }
}
