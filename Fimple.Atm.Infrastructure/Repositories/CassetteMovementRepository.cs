using Fimple.Atm.Domain.Entities;
using Fimple.Atm.Domain.Interfaces;
using Fimple.Atm.Domain.Models;
using Fimple.Atm.Infrastructure.PostgreSQL;
using Microsoft.EntityFrameworkCore;

namespace Fimple.Atm.Infrastructure.Repositories
{
    public class CassetteMovementRepository : ICassetteMovementRepository
    {
        private readonly AppDbContext _appDbContext;

        public CassetteMovementRepository(AppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }

        public async Task<List<CassetteMovementSum>> GetSums(IReadOnlyCollection<Guid> cassetteIds)
        {
            var cassetteIdList = cassetteIds.ToList();
            return await _appDbContext.CassetteMovements
                .AsNoTracking()
                .Where(movement => cassetteIdList.Contains(movement.CassetteId))
                .GroupBy(movement => new { movement.CassetteId, movement.Currency, movement.Denomination })
                .Select(group => new CassetteMovementSum(
                    group.Key.CassetteId,
                    group.Key.Currency,
                    group.Key.Denomination,
                    group.Sum(movement => movement.NoteCountDelta),
                    group.Sum(movement => movement.ReservedCountDelta)))
                .ToListAsync();
        }

        public void Add(CassetteMovement movement)
        {
            _appDbContext.CassetteMovements.Add(movement);
        }
    }
}
