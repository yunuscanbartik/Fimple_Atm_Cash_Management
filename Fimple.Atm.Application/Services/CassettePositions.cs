using Fimple.Atm.Domain.Entities;

namespace Fimple.Atm.Application.Services
{
    internal static class CassettePositions
    {
        public static Dictionary<Guid, int> Build(IEnumerable<Cassette> cassettes)
        {
            return cassettes.ToDictionary(cassette => cassette.Id, cassette => cassette.Position);
        }

        public static int Get(IReadOnlyDictionary<Guid, int> positions, Guid cassetteId)
        {
            if (!positions.TryGetValue(cassetteId, out var position))
            {
                throw new InvalidOperationException($"Cassette {cassetteId} does not belong to the ATM that was loaded.");
            }

            return position;
        }
    }
}
