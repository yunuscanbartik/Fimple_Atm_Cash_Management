using System.Text.Json;
using System.Text.Json.Serialization;
using Fimple.Atm.Api.Filters;
using Fimple.Atm.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace Fimple.Atm.Api.Extensions
{
    public static class ServiceCollectionExtensions
    {
        private const string ERROR_CODE_EXTENSION_KEY = "errorCode";
        private const string TRACE_ID_EXTENSION_KEY = "traceId";
        private const string PROBLEM_TYPE_PREFIX = "urn:fimple:atm:error:";
        private const string VALIDATION_FAILED_TITLE = "One or more validation errors occurred.";

        public static IMvcBuilder AddAtmControllers(this IServiceCollection services)
        {
            return services
                .AddControllers(options =>
                {
                    options.Filters.Add<UserHeaderActionFilter>();
                })
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseUpper));
                })
                .ConfigureApiBehaviorOptions(options =>
                {
                    options.InvalidModelStateResponseFactory = CreateValidationProblemResponse;
                });
        }

        private static IActionResult CreateValidationProblemResponse(ActionContext actionContext)
        {
            var problemDetails = new ValidationProblemDetails(actionContext.ModelState)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = VALIDATION_FAILED_TITLE,
                Type = PROBLEM_TYPE_PREFIX + ErrorCodes.VALIDATION_FAILED,
                Instance = actionContext.HttpContext.Request.Path
            };
            problemDetails.Extensions[ERROR_CODE_EXTENSION_KEY] = ErrorCodes.VALIDATION_FAILED;
            problemDetails.Extensions[TRACE_ID_EXTENSION_KEY] = actionContext.HttpContext.TraceIdentifier;

            return new BadRequestObjectResult(problemDetails)
            {
                ContentTypes = { "application/problem+json" }
            };
        }
    }
}
