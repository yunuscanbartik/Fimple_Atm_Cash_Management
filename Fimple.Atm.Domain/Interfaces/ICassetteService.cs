using Fimple.Atm.Domain.Models;

namespace Fimple.Atm.Domain.Interfaces
{
    public interface ICassetteService
    {
        public Task<AtmModel.AtmDetail> ConfigureCassettes(AtmModel.ConfigureCassettes.Request request);
        public Task<AtmModel.AtmDetail> LoadCash(AtmModel.LoadCash.Request request);
        public Task<AtmModel.AtmDetail> UnloadCash(AtmModel.UnloadCash.Request request);
    }
}
