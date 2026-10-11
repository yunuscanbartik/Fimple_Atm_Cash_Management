using Fimple.Atm.Domain.Models;

namespace Fimple.Atm.Domain.Interfaces
{
    public interface ICassetteService
    {
        public Task<AtmModel.ConfigureCassettes.Return> ConfigureCassettes(AtmModel.ConfigureCassettes.Request request);
        public Task<AtmModel.LoadCash.Return> LoadCash(AtmModel.LoadCash.Request request);
        public Task<AtmModel.UnloadCash.Return> UnloadCash(AtmModel.UnloadCash.Request request);
    }
}
