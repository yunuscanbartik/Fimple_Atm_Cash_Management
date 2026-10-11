using Fimple.Atm.Domain.Models;

namespace Fimple.Atm.Domain.Interfaces
{
    public interface IReportService
    {
        public Task<ReportModel.Operations.Return> GetOperationsReport(ReportModel.Operations.Request request);
    }
}
