using Fimple.Atm.Domain.Constants;
using Fimple.Atm.Domain.Entities;
using Fimple.Atm.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Fimple.Atm.Infrastructure.PostgreSQL
{
    public static class AppDbContextExtensions
    {
        public static async Task<AtmMachine> LoadAtmForUpdate(this AppDbContext appDbContext, Guid atmId)
        {
            var lockedAtmIds = await appDbContext.Database
                .SqlQuery<Guid>($"SELECT \"Id\" AS \"Value\" FROM \"t_Atms\" WHERE \"Id\" = {atmId} FOR UPDATE")
                .ToListAsync();
            if (lockedAtmIds.Count == 0)
            {
                throw new NotFoundException(ErrorCodes.AtmNotFound, $"ATM not found: {atmId}");
            }

            return await appDbContext.AtmMachines
                .Include(atm => atm.Cassettes)
                .ThenInclude(cassette => cassette.Contents)
                .SingleAsync(atm => atm.Id == atmId);
        }

        public static async Task LockCardForUpdate(this AppDbContext appDbContext, string cardHash)
        {
            await appDbContext.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({cardHash}, 0))");
        }
    }
}
