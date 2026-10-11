using Fimple.Atm.Domain.Enums;

namespace Fimple.Atm.Application.Services
{
    public static class NoteSelector
    {
        private const int UNREACHABLE = int.MaxValue;

        private sealed record DenominationStock(Denomination Denomination, int Value, int AvailableCount);

        public static NoteSelectionResult Select(IReadOnlyCollection<NoteStock> noteStocks, int requestedAmount)
        {
            if (requestedAmount <= 0) throw new ArgumentOutOfRangeException(nameof(requestedAmount), "The requested amount must be positive.");

            var denominationStocks = GroupByDenomination(noteStocks);
            if (denominationStocks.Count == 0) return new NoteSelectionResult();

            var unit = denominationStocks.Select(stock => stock.Value).Aggregate(GreatestCommonDivisor);
            var totalUnits = denominationStocks.Sum(stock => stock.AvailableCount * (stock.Value / unit));
            var table = BuildMinimumNoteTable(denominationStocks, unit, totalUnits);

            var isAligned = requestedAmount % unit == 0;
            var requestedUnits = requestedAmount / unit;
            var isExactPayable = isAligned && requestedUnits <= totalUnits && IsPayable(table, requestedUnits);
            if (isExactPayable)
            {
                return new NoteSelectionResult { Exact = BuildBreakdown(table, requestedUnits, unit, denominationStocks, noteStocks) };
            }

            var lowerUnits = FindLargestPayableBelow(table, isAligned ? requestedUnits - 1 : requestedUnits, totalUnits);
            var upperUnits = FindSmallestPayableAbove(table, requestedUnits, totalUnits);
            return new NoteSelectionResult
            {
                Lower = lowerUnits.HasValue ? BuildBreakdown(table, lowerUnits.Value, unit, denominationStocks, noteStocks) : null,
                Upper = upperUnits.HasValue ? BuildBreakdown(table, upperUnits.Value, unit, denominationStocks, noteStocks) : null
            };
        }

        private static List<DenominationStock> GroupByDenomination(IReadOnlyCollection<NoteStock> noteStocks)
        {
            return noteStocks
                .Where(stock => stock.AvailableCount > 0)
                .GroupBy(stock => stock.Denomination)
                .Select(group => new DenominationStock(group.Key, (int)group.Key, group.Sum(stock => stock.AvailableCount)))
                .OrderBy(stock => stock.Value)
                .ToList();
        }

        private static MinimumNoteTable BuildMinimumNoteTable(List<DenominationStock> denominationStocks, int unit, int totalUnits)
        {
            var layerCount = denominationStocks.Count;
            var minimumNotes = new int[layerCount + 1][];
            var chosenCounts = new int[layerCount][];

            minimumNotes[0] = Enumerable.Repeat(UNREACHABLE, totalUnits + 1).ToArray();
            minimumNotes[0][0] = 0;

            for (var layer = 0; layer < layerCount; layer++)
            {
                var stock = denominationStocks[layer];
                var stepUnits = stock.Value / unit;
                var previous = minimumNotes[layer];
                var current = new int[totalUnits + 1];
                var chosen = new int[totalUnits + 1];

                for (var amountUnits = 0; amountUnits <= totalUnits; amountUnits++)
                {
                    var best = UNREACHABLE;
                    var bestCount = 0;
                    var maxCount = Math.Min(stock.AvailableCount, amountUnits / stepUnits);

                    for (var count = maxCount; count >= 0; count--)
                    {
                        var previousNotes = previous[amountUnits - count * stepUnits];
                        if (previousNotes == UNREACHABLE) continue;

                        var candidate = previousNotes + count;
                        if (candidate < best)
                        {
                            best = candidate;
                            bestCount = count;
                        }
                    }

                    current[amountUnits] = best;
                    chosen[amountUnits] = bestCount;
                }

                minimumNotes[layer + 1] = current;
                chosenCounts[layer] = chosen;
            }

            return new MinimumNoteTable(minimumNotes[layerCount], chosenCounts);
        }

        private static bool IsPayable(MinimumNoteTable table, int amountUnits)
        {
            return table.FinalMinimumNotes[amountUnits] != UNREACHABLE;
        }

        private static int? FindLargestPayableBelow(MinimumNoteTable table, int startUnits, int totalUnits)
        {
            for (var amountUnits = Math.Min(startUnits, totalUnits); amountUnits >= 1; amountUnits--)
            {
                if (IsPayable(table, amountUnits)) return amountUnits;
            }

            return null;
        }

        private static int? FindSmallestPayableAbove(MinimumNoteTable table, int requestedUnits, int totalUnits)
        {
            for (var amountUnits = requestedUnits + 1; amountUnits <= totalUnits; amountUnits++)
            {
                if (IsPayable(table, amountUnits)) return amountUnits;
            }

            return null;
        }

        private static NoteBreakdown BuildBreakdown(
            MinimumNoteTable table,
            int amountUnits,
            int unit,
            List<DenominationStock> denominationStocks,
            IReadOnlyCollection<NoteStock> noteStocks)
        {
            var allocations = new List<NoteAllocation>();
            var remainingUnits = amountUnits;

            for (var layer = denominationStocks.Count - 1; layer >= 0; layer--)
            {
                var stock = denominationStocks[layer];
                var count = table.ChosenCounts[layer][remainingUnits];
                remainingUnits -= count * (stock.Value / unit);
                if (count == 0) continue;

                allocations.AddRange(AllocateAcrossCassettes(stock.Denomination, count, noteStocks));
            }

            var orderedAllocations = allocations
                .OrderByDescending(allocation => (int)allocation.Denomination)
                .ThenBy(allocation => allocation.CassetteId)
                .ToList();
            return new NoteBreakdown { Amount = amountUnits * unit, Allocations = orderedAllocations };
        }

        private static IEnumerable<NoteAllocation> AllocateAcrossCassettes(
            Denomination denomination,
            int noteCount,
            IReadOnlyCollection<NoteStock> noteStocks)
        {
            var cassettes = noteStocks
                .Where(stock => stock.Denomination == denomination && stock.AvailableCount > 0)
                .OrderByDescending(stock => (decimal)stock.AvailableCount / stock.Capacity)
                .ThenBy(stock => stock.Position);

            var remaining = noteCount;
            foreach (var cassette in cassettes)
            {
                if (remaining == 0) yield break;

                var taken = Math.Min(cassette.AvailableCount, remaining);
                remaining -= taken;
                yield return new NoteAllocation(cassette.CassetteId, denomination, taken);
            }
        }

        private static int GreatestCommonDivisor(int first, int second)
        {
            while (second != 0)
            {
                (first, second) = (second, first % second);
            }

            return first;
        }

        private sealed record MinimumNoteTable(int[] FinalMinimumNotes, int[][] ChosenCounts);
    }
}
