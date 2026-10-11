using Fimple.Atm.Domain.Interfaces;
using Fimple.Atm.Domain.Models;
using Microsoft.AspNetCore.Mvc;

namespace Fimple.Atm.Api.Controllers
{
    [ApiController]
    [Route("api/reports")]
    [Tags("Report")]
    public class ReportsController : ControllerBase
    {
        private readonly IReportService _reportService;

        public ReportsController(IReportService reportService)
        {
            _reportService = reportService;
        }

        /// <summary>
        /// Operations report: per ATM and per cassette the note count, reserved count and available count, the
        /// totals per currency, and the overall total in TRY. Pass atmId to limit the report to one ATM.
        /// </summary>
        [HttpGet("operations")]
        [ProducesResponseType(typeof(ReportModel.Operations.Return), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 404)]
        public async Task<ActionResult<ReportModel.Operations.Return>> GetOperationsReport([FromQuery] ReportModel.Operations.Request request)
        {
            var result = await _reportService.GetOperationsReport(request);
            return Ok(result);
        }
    }
}
