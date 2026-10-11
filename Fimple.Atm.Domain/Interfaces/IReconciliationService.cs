using Fimple.Atm.Domain.Models;

namespace Fimple.Atm.Domain.Interfaces
{
    public interface IReconciliationService
    {
        public Task<ReconciliationModel.Get.Return> GetReconciliation(ReconciliationModel.Get.Request request);
        public Task<ReconciliationModel.GetDetail.Return> GetReconciliationDetail(ReconciliationModel.GetDetail.Request request);
        public Task<ReconciliationModel.Add.Return> AddReconciliation(ReconciliationModel.Add.Request request);
        public Task<ReconciliationModel.Approve.Return> ApproveReconciliation(ReconciliationModel.Approve.Request request);
    }
}
