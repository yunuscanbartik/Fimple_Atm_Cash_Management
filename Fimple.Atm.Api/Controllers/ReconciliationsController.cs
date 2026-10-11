using Fimple.Atm.Domain.Interfaces;
using Fimple.Atm.Domain.Models;
using Microsoft.AspNetCore.Mvc;

namespace Fimple.Atm.Api.Controllers
{
    [ApiController]
    [Route("api/atms/{atmId:guid}")]
    [Tags("Reconciliation")]
    public class ReconciliationsController : ControllerBase
    {
        private readonly IReconciliationService _reconciliationService;
        private readonly IConsistencyService _consistencyService;

        public ReconciliationsController(IReconciliationService reconciliationService, IConsistencyService consistencyService)
        {
            _reconciliationService = reconciliationService;
            _consistencyService = consistencyService;
        }

        /// <summary>
        /// Lists the reconciliations of an ATM, newest first.
        /// </summary>
        [HttpGet("reconciliations")]
        [ProducesResponseType(typeof(List<ReconciliationModel.ReconciliationItem>), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 404)]
        public async Task<ActionResult<List<ReconciliationModel.ReconciliationItem>>> GetReconciliation(Guid atmId)
        {
            var request = new ReconciliationModel.Get.Request { AtmId = atmId };
            var result = await _reconciliationService.GetReconciliation(request);
            return Ok(result.Items);
        }

        /// <summary>
        /// Returns one reconciliation with its per-cassette lines.
        /// </summary>
        [HttpGet("reconciliations/{reconciliationId:guid}")]
        [ProducesResponseType(typeof(ReconciliationModel.GetDetail.Return), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 404)]
        public async Task<ActionResult<ReconciliationModel.GetDetail.Return>> GetReconciliationDetail(Guid atmId, Guid reconciliationId)
        {
            var request = new ReconciliationModel.GetDetail.Request { AtmId = atmId, ReconciliationId = reconciliationId };
            var result = await _reconciliationService.GetReconciliationDetail(request);
            return Ok(result);
        }

        /// <summary>
        /// Enters the hand-counted notes of every cassette while the ATM is in MAINTENANCE. The differences are
        /// written as RECONCILIATION_ADJUSTMENT movements. When the total value of the differences exceeds
        /// TRY 1,000 the reconciliation waits for approval by a different user. Requires the X-User header.
        /// </summary>
        [HttpPost("reconciliations")]
        [ProducesResponseType(typeof(ReconciliationModel.Add.Return), 201)]
        [ProducesResponseType(typeof(ProblemDetails), 400)]
        [ProducesResponseType(typeof(ProblemDetails), 409)]
        [ProducesResponseType(typeof(ProblemDetails), 422)]
        public async Task<ActionResult<ReconciliationModel.Add.Return>> AddReconciliation(Guid atmId, [FromBody] ReconciliationModel.Add.Request request)
        {
            request.AtmId = atmId;
            var result = await _reconciliationService.AddReconciliation(request);
            return CreatedAtAction(nameof(GetReconciliationDetail), new { atmId, reconciliationId = result.Id }, result);
        }

        /// <summary>
        /// Approves a reconciliation that is awaiting approval. The approver must be a different user than the one
        /// who entered the counts. Requires the X-User header.
        /// </summary>
        [HttpPost("reconciliations/{reconciliationId:guid}/approve")]
        [ProducesResponseType(typeof(ReconciliationModel.Approve.Return), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 400)]
        [ProducesResponseType(typeof(ProblemDetails), 403)]
        [ProducesResponseType(typeof(ProblemDetails), 404)]
        [ProducesResponseType(typeof(ProblemDetails), 409)]
        public async Task<ActionResult<ReconciliationModel.Approve.Return>> ApproveReconciliation(Guid atmId, Guid reconciliationId)
        {
            var request = new ReconciliationModel.Approve.Request { AtmId = atmId, ReconciliationId = reconciliationId };
            request.SetUserName(Request.Headers[Filters.UserHeaderActionFilter.UserHeaderName].FirstOrDefault()?.Trim());
            var result = await _reconciliationService.ApproveReconciliation(request);
            return Ok(result);
        }

        /// <summary>
        /// Compares the stored cassette balances with the balances calculated from the movement ledger and reports
        /// every difference.
        /// </summary>
        [HttpGet("consistency")]
        [ProducesResponseType(typeof(ConsistencyModel.Check.Return), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 404)]
        public async Task<ActionResult<ConsistencyModel.Check.Return>> CheckConsistency(Guid atmId)
        {
            var request = new ConsistencyModel.Check.Request { AtmId = atmId };
            var result = await _consistencyService.CheckConsistency(request);
            return Ok(result);
        }
    }
}
