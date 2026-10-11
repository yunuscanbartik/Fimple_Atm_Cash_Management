using Fimple.Atm.Domain.Constants;
using Fimple.Atm.Domain.Interfaces;
using Fimple.Atm.Domain.Models;
using Microsoft.AspNetCore.Mvc;

namespace Fimple.Atm.Api.Controllers
{
    [ApiController]
    [Route("api/atms/{atmId:guid}/withdrawals")]
    [Tags("Withdrawal")]
    public class WithdrawalsController : ControllerBase
    {
        private readonly IWithdrawalService _withdrawalService;

        public WithdrawalsController(IWithdrawalService withdrawalService)
        {
            _withdrawalService = withdrawalService;
        }

        /// <summary>
        /// Quotes a withdrawal. Returns the exact note breakdown, or a lower and an upper alternative when the
        /// amount cannot be paid exactly, and reserves the offered notes for 60 seconds.
        /// </summary>
        [HttpPost("quotes")]
        [ProducesResponseType(typeof(WithdrawalModel.QuoteItem), 201)]
        [ProducesResponseType(typeof(ProblemDetails), 409)]
        [ProducesResponseType(typeof(ProblemDetails), 422)]
        public async Task<ActionResult<WithdrawalModel.QuoteItem>> CreateQuote(Guid atmId, [FromBody] WithdrawalModel.CreateQuote.Request request)
        {
            request.AtmId = atmId;
            var result = await _withdrawalService.CreateQuote(request);
            return CreatedAtAction(nameof(GetQuote), new { atmId, quoteId = result.QuoteId }, result);
        }

        /// <summary>
        /// Returns a quote. A quote past its 60 seconds is reported as EXPIRED.
        /// </summary>
        [HttpGet("quotes/{quoteId:guid}")]
        [ProducesResponseType(typeof(WithdrawalModel.QuoteItem), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 404)]
        public async Task<ActionResult<WithdrawalModel.QuoteItem>> GetQuote(Guid atmId, Guid quoteId)
        {
            var request = new WithdrawalModel.GetQuote.Request { AtmId = atmId, QuoteId = quoteId };
            var result = await _withdrawalService.GetQuote(request);
            return Ok(result);
        }

        /// <summary>
        /// Confirms a quote and debits the cash. Requires an Idempotency-Key header; repeating the request with the
        /// same key returns the same result without debiting twice. The option field picks EXACT, LOWER or UPPER
        /// and may be omitted when the quote offers a single option.
        /// </summary>
        [HttpPost("quotes/{quoteId:guid}/confirm")]
        [ProducesResponseType(typeof(WithdrawalModel.ConfirmItem), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 400)]
        [ProducesResponseType(typeof(ProblemDetails), 404)]
        [ProducesResponseType(typeof(ProblemDetails), 409)]
        [ProducesResponseType(typeof(ProblemDetails), 422)]
        public async Task<ActionResult<WithdrawalModel.ConfirmItem>> ConfirmQuote(
            Guid atmId,
            Guid quoteId,
            [FromHeader(Name = WithdrawalRules.IdempotencyKeyHeaderName)] string? idempotencyKey,
            [FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] WithdrawalModel.ConfirmQuote.Request? request)
        {
            var confirmRequest = request ?? new WithdrawalModel.ConfirmQuote.Request();
            confirmRequest.AtmId = atmId;
            confirmRequest.QuoteId = quoteId;
            confirmRequest.IdempotencyKey = idempotencyKey;

            var result = await _withdrawalService.ConfirmQuote(confirmRequest);
            return Ok(result);
        }

        /// <summary>
        /// Cancels a pending quote and releases its reserved notes.
        /// </summary>
        [HttpPost("quotes/{quoteId:guid}/cancel")]
        [ProducesResponseType(typeof(WithdrawalModel.QuoteItem), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 404)]
        [ProducesResponseType(typeof(ProblemDetails), 409)]
        public async Task<ActionResult<WithdrawalModel.QuoteItem>> CancelQuote(Guid atmId, Guid quoteId)
        {
            var request = new WithdrawalModel.CancelQuote.Request { AtmId = atmId, QuoteId = quoteId };
            var result = await _withdrawalService.CancelQuote(request);
            return Ok(result);
        }
    }
}
