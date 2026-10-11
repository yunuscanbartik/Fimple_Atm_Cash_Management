using Fimple.Atm.Domain.Constants;
using Fimple.Atm.Domain.Entities;
using Fimple.Atm.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Fimple.Atm.Infrastructure.PostgreSQL
{
    public static class AppDbContextExtensions
    {
        private const string LOCK_ATM_SQL = $"SELECT \"Id\" AS \"Value\" FROM \"{TableNames.ATMS}\" WHERE \"Id\" = {{0}} FOR UPDATE";
        private const string LOCK_CARD_SQL = "SELECT pg_advisory_xact_lock(hashtextextended({0}, 0))";

        public static async Task<AtmMachine> LoadAtmForUpdate(this AppDbContext appDbContext, Guid atmId)
        {
            var lockedAtmIds = await appDbContext.Database
                .SqlQueryRaw<Guid>(LOCK_ATM_SQL, atmId)
                .ToListAsync();
            if (lockedAtmIds.Count == 0)
            {
                throw new NotFoundException(ErrorCodes.ATM_NOT_FOUND, $"ATM not found: {atmId}");
            }

            return await appDbContext.AtmMachines
                .Include(atm => atm.Cassettes)
                .ThenInclude(cassette => cassette.Contents)
                .SingleAsync(atm => atm.Id == atmId);
        }

        public static async Task LockCardForUpdate(this AppDbContext appDbContext, string cardHash)
        {
            await appDbContext.Database.ExecuteSqlRawAsync(LOCK_CARD_SQL, cardHash);
        }
    }
}
