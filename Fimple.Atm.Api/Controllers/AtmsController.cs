using Fimple.Atm.Domain.Interfaces;
using Fimple.Atm.Domain.Models;
using Microsoft.AspNetCore.Mvc;

namespace Fimple.Atm.Api.Controllers
{
    [ApiController]
    [Route("api/atms")]
    [Tags("Atm")]
    public class AtmsController : ControllerBase
    {
        private readonly IAtmService _atmService;
        private readonly ICassetteService _cassetteService;

        public AtmsController(IAtmService atmService, ICassetteService cassetteService)
        {
            _atmService = atmService;
            _cassetteService = cassetteService;
        }

        /// <summary>
        /// Returns all ATMs in the network.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<AtmModel.AtmItem>), 200)]
        public async Task<ActionResult<List<AtmModel.AtmItem>>> GetAtm([FromQuery] AtmModel.Get.Request request)
        {
            var result = await _atmService.GetAtm(request);
            return Ok(result.Items);
        }

        /// <summary>
        /// Returns an ATM with its cassettes, note counts per denomination and totals per currency.
        /// </summary>
        [HttpGet("{atmId:guid}")]
        [ProducesResponseType(typeof(AtmModel.GetDetail.Return), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 404)]
        public async Task<ActionResult<AtmModel.GetDetail.Return>> GetAtmDetail(Guid atmId)
        {
            var request = new AtmModel.GetDetail.Request { AtmId = atmId };
            var result = await _atmService.GetAtmDetail(request);
            return Ok(result);
        }

        /// <summary>
        /// Registers a new ATM in the UNCONFIGURED state.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(AtmModel.Add.Return), 201)]
        [ProducesResponseType(typeof(ProblemDetails), 409)]
        public async Task<ActionResult<AtmModel.Add.Return>> AddAtm([FromBody] AtmModel.Add.Request request)
        {
            var result = await _atmService.AddAtm(request);
            return CreatedAtAction(nameof(GetAtmDetail), new { atmId = result.Id }, result);
        }

        /// <summary>
        /// Configures the five cassettes of an ATM: one deposit cassette and four recycling cassettes.
        /// Allowed during initial setup or in MAINTENANCE.
        /// </summary>
        [HttpPut("{atmId:guid}/cassettes")]
        [ProducesResponseType(typeof(AtmModel.ConfigureCassettes.Return), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 409)]
        [ProducesResponseType(typeof(ProblemDetails), 422)]
        public async Task<ActionResult<AtmModel.ConfigureCassettes.Return>> ConfigureCassettes(Guid atmId, [FromBody] AtmModel.ConfigureCassettes.Request request)
        {
            request.AtmId = atmId;
            var result = await _cassetteService.ConfigureCassettes(request);
            return Ok(result);
        }

        /// <summary>
        /// Records cash loaded into the cassettes by hand.
        /// </summary>
        [HttpPost("{atmId:guid}/load")]
        [ProducesResponseType(typeof(AtmModel.LoadCash.Return), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 409)]
        [ProducesResponseType(typeof(ProblemDetails), 422)]
        public async Task<ActionResult<AtmModel.LoadCash.Return>> LoadCash(Guid atmId, [FromBody] AtmModel.LoadCash.Request request)
        {
            request.AtmId = atmId;
            var result = await _cassetteService.LoadCash(request);
            return Ok(result);
        }

        /// <summary>
        /// Records cash removed from the cassettes by hand.
        /// </summary>
        [HttpPost("{atmId:guid}/unload")]
        [ProducesResponseType(typeof(AtmModel.UnloadCash.Return), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 409)]
        [ProducesResponseType(typeof(ProblemDetails), 422)]
        public async Task<ActionResult<AtmModel.UnloadCash.Return>> UnloadCash(Guid atmId, [FromBody] AtmModel.UnloadCash.Request request)
        {
            request.AtmId = atmId;
            var result = await _cassetteService.UnloadCash(request);
            return Ok(result);
        }

        /// <summary>
        /// Puts a loaded ATM (or one in MAINTENANCE) into service.
        /// </summary>
        [HttpPost("{atmId:guid}/put-in-service")]
        [ProducesResponseType(typeof(AtmModel.PutInService.Return), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 409)]
        public async Task<ActionResult<AtmModel.PutInService.Return>> PutInService(Guid atmId)
        {
            var request = new AtmModel.PutInService.Request { AtmId = atmId };
            var result = await _atmService.PutInService(request);
            return Ok(result);
        }

        /// <summary>
        /// Takes an ATM that is in service into MAINTENANCE.
        /// </summary>
        [HttpPost("{atmId:guid}/enter-maintenance")]
        [ProducesResponseType(typeof(AtmModel.EnterMaintenance.Return), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 409)]
        public async Task<ActionResult<AtmModel.EnterMaintenance.Return>> EnterMaintenance(Guid atmId)
        {
            var request = new AtmModel.EnterMaintenance.Request { AtmId = atmId };
            var result = await _atmService.EnterMaintenance(request);
            return Ok(result);
        }
    }
}
