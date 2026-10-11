using Fimple.Atm.Domain.Models;

namespace Fimple.Atm.Domain.Interfaces
{
    public interface IReconciliationService
    {
        public Task<ReconciliationModel.Get.Return> GetReconciliation(ReconciliationModel.Get.Request request);
        public Task<ReconciliationModel.ReconciliationItem> GetReconciliationDetail(ReconciliationModel.GetDetail.Request request);
        public Task<ReconciliationModel.ReconciliationItem> AddReconciliation(ReconciliationModel.Add.Request request);
        public Task<ReconciliationModel.ReconciliationItem> ApproveReconciliation(ReconciliationModel.Approve.Request request);
    }
}
