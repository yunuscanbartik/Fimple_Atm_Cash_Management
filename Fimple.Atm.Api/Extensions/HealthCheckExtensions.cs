using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Fimple.Atm.Api.Extensions
{
    public static class HealthCheckExtensions
    {
        private const string ReadinessPath = "/health";
        private const string LivenessPath = "/health/live";
        private const string HealthJsonContentType = "application/json";

        private static readonly JsonSerializerOptions HealthJsonOptions = new(JsonSerializerDefaults.Web);

        public static WebApplication MapAtmHealthChecks(this WebApplication app)
        {
            app.MapHealthChecks(ReadinessPath, new HealthCheckOptions { ResponseWriter = WriteResponse });
            app.MapHealthChecks(LivenessPath, new HealthCheckOptions { Predicate = _ => false, ResponseWriter = WriteResponse });
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

            httpContext.Response.ContentType = HealthJsonContentType;
            return httpContext.Response.WriteAsync(JsonSerializer.Serialize(response, HealthJsonOptions));
        }
    }
}
