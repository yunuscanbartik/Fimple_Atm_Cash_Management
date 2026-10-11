using Fimple.Atm.Domain.Interfaces;
using Fimple.Atm.Domain.Models;
using Microsoft.AspNetCore.Mvc;

namespace Fimple.Atm.Api.Controllers
{
    [ApiController]
    [Route("api/atms/{atmId:guid}/deposits")]
    [Tags("Deposit")]
    public class DepositsController : ControllerBase
    {
        private readonly IDepositService _depositService;

        public DepositsController(IDepositService depositService)
        {
            _depositService = depositService;
        }

        /// <summary>
        /// Records a deposit. Notes with a matching recycling cassette go to it; the rest, and any recycling
        /// overflow, go to the deposit cassette. Notes that fit nowhere and unrecognised notes are returned
        /// to the customer. The response lists the destination cassette of every accepted note.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(DepositModel.Add.Return), 201)]
        [ProducesResponseType(typeof(ProblemDetails), 409)]
        [ProducesResponseType(typeof(ProblemDetails), 422)]
        public async Task<ActionResult<DepositModel.Add.Return>> AddDeposit(Guid atmId, [FromBody] DepositModel.Add.Request request)
        {
            request.AtmId = atmId;
            var result = await _depositService.AddDeposit(request);
            return StatusCode(StatusCodes.Status201Created, result);
        }
    }
}
