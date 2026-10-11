using Fimple.Atm.Domain.Models;

namespace Fimple.Atm.Domain.Interfaces
{
    public interface IAtmService
    {
        public Task<AtmModel.Get.Return> GetAtm(AtmModel.Get.Request request);
        public Task<AtmModel.AtmDetail> GetAtmDetail(AtmModel.GetDetail.Request request);
        public Task<AtmModel.Add.Return> AddAtm(AtmModel.Add.Request request);
        public Task<AtmModel.PutInService.Return> PutInService(AtmModel.PutInService.Request request);
        public Task<AtmModel.EnterMaintenance.Return> EnterMaintenance(AtmModel.EnterMaintenance.Request request);
    }
}
