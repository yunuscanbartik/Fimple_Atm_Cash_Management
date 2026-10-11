using Fimple.Atm.Domain.Entities;
using Fimple.Atm.Domain.Interfaces;
using Fimple.Atm.Infrastructure.PostgreSQL;
using Microsoft.EntityFrameworkCore;

namespace Fimple.Atm.Infrastructure.Repositories
{
    public class AtmRepository : IAtmRepository
    {
        private readonly AppDbContext _appDbContext;

        public AtmRepository(AppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }

        public async Task<List<AtmMachine>> GetAllReadOnly()
        {
            return await _appDbContext.AtmMachines
                .AsNoTracking()
                .OrderBy(atm => atm.Name)
                .ToListAsync();
        }

        public async Task<List<AtmMachine>> GetAllWithCassettesReadOnly(Guid? atmId)
        {
            var query = _appDbContext.AtmMachines
                .AsNoTracking()
                .Include(atm => atm.Cassettes)
                .ThenInclude(cassette => cassette.Contents)
                .AsQueryable();

            if (atmId.HasValue)
            {
                query = query.Where(atm => atm.Id == atmId.Value);
            }

            return await query.OrderBy(atm => atm.Name).ToListAsync();
        }

        public async Task<AtmMachine?> FindWithCassettesReadOnly(Guid atmId)
        {
            return await _appDbContext.AtmMachines
                .AsNoTracking()
                .Include(atm => atm.Cassettes)
                .ThenInclude(cassette => cassette.Contents)
                .FirstOrDefaultAsync(atm => atm.Id == atmId);
        }

        public async Task<AtmMachine> LoadForUpdate(Guid atmId)
        {
            return await _appDbContext.LoadAtmForUpdate(atmId);
        }

        public async Task<bool> Exists(Guid atmId)
        {
            return await _appDbContext.AtmMachines.AnyAsync(atm => atm.Id == atmId);
        }

        public async Task<bool> NameExists(string name)
        {
            return await _appDbContext.AtmMachines.AnyAsync(atm => atm.Name == name);
        }

        public async Task<List<Cassette>> GetCassettesReadOnly(Guid atmId)
        {
            return await _appDbContext.Cassettes
                .AsNoTracking()
                .Where(cassette => cassette.AtmId == atmId)
                .ToListAsync();
        }

        public async Task<List<Cassette>> GetCassettesWithContentsReadOnly(Guid atmId)
        {
            return await _appDbContext.Cassettes
                .AsNoTracking()
                .Include(cassette => cassette.Contents)
                .Where(cassette => cassette.AtmId == atmId)
                .ToListAsync();
        }

        public void Add(AtmMachine atm)
        {
            _appDbContext.AtmMachines.Add(atm);
        }

        public void RemoveContents(IEnumerable<CassetteContent> contents)
        {
            _appDbContext.CassetteContents.RemoveRange(contents);
        }
    }
}
