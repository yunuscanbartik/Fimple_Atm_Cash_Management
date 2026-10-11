using Fimple.Atm.Domain.Enums;

namespace Fimple.Atm.Domain.Models
{
    public record CassetteMovementSum(Guid CassetteId, Currency Currency, Denomination Denomination, int NoteCount, int ReservedCount);
}
