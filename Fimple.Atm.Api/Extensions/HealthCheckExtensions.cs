using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Fimple.Atm.Api.Extensions
{
    public static class HealthCheckExtensions
    {
        private const string READINESS_PATH = "/health";
        private const string LIVENESS_PATH = "/health/live";
        private const string HEALTH_JSON_CONTENT_TYPE = "application/json";

        private static readonly JsonSerializerOptions HealthJsonOptions = new(JsonSerializerDefaults.Web);

        public static WebApplication MapAtmHealthChecks(this WebApplication app)
        {
            app.MapHealthChecks(READINESS_PATH, new HealthCheckOptions { ResponseWriter = WriteResponse });
            app.MapHealthChecks(LIVENESS_PATH, new HealthCheckOptions { Predicate = _ => false, ResponseWriter = WriteResponse });
            return app;
        }

        private static Task WriteResponse(HttpContext httpContext, HealthReport healthReport)
        {
            var response = new
            {
                status = healthReport.Status.ToString(),
                totalDurationMs = (int)healthReport.TotalDuration.TotalMilliseconds,
                checks = healthReport.Entries.Select(entry => new
                {
                    name = entry.Key,
                    status = entry.Value.Status.ToString(),
                    description = entry.Value.Description,
                    error = entry.Value.Exception?.Message
                })
            };

            httpContext.Response.ContentType = HEALTH_JSON_CONTENT_TYPE;
            return httpContext.Response.WriteAsync(JsonSerializer.Serialize(response, HealthJsonOptions));
        }
    }
}
