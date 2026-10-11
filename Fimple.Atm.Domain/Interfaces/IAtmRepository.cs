using Fimple.Atm.Domain.Entities;

namespace Fimple.Atm.Domain.Interfaces
{
    public interface IAtmRepository
    {
        public Task<List<AtmMachine>> GetAllReadOnly();
        public Task<List<AtmMachine>> GetAllWithCassettesReadOnly(Guid? atmId);
        public Task<AtmMachine?> FindWithCassettesReadOnly(Guid atmId);
        public Task<AtmMachine> LoadForUpdate(Guid atmId);
        public Task<bool> Exists(Guid atmId);
        public Task<bool> NameExists(string name);
        public Task<List<Cassette>> GetCassettesReadOnly(Guid atmId);
        public Task<List<Cassette>> GetCassettesWithContentsReadOnly(Guid atmId);
        public void Add(AtmMachine atm);
        public void RemoveContents(IEnumerable<CassetteContent> contents);
    }
}
