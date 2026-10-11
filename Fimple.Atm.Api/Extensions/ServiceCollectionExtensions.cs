using System.Text.Json;
using System.Text.Json.Serialization;
using Fimple.Atm.Api.Filters;
using Fimple.Atm.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace Fimple.Atm.Api.Extensions
{
    public static class ServiceCollectionExtensions
    {
        private const string ErrorCodeExtensionKey = "errorCode";
        private const string TraceIdExtensionKey = "traceId";
        private const string ProblemTypePrefix = "urn:fimple:atm:error:";
        private const string ValidationFailedTitle = "One or more validation errors occurred.";

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
                Title = ValidationFailedTitle,
                Type = ProblemTypePrefix + ErrorCodes.ValidationFailed,
                Instance = actionContext.HttpContext.Request.Path
            };
            problemDetails.Extensions[ErrorCodeExtensionKey] = ErrorCodes.ValidationFailed;
            problemDetails.Extensions[TraceIdExtensionKey] = actionContext.HttpContext.TraceIdentifier;

            return new BadRequestObjectResult(problemDetails)
            {
                ContentTypes = { "application/problem+json" }
            };
        }
    }
}
