using Fimple.Atm.Domain.Entities;
using Fimple.Atm.Domain.Models;

namespace Fimple.Atm.Domain.Interfaces
{
    public interface ICassetteMovementRepository
    {
        public Task<List<CassetteMovementSum>> GetSums(IReadOnlyCollection<Guid> cassetteIds);
        public void Add(CassetteMovement movement);
    }
}
