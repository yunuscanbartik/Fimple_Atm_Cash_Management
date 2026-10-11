using System.Net;
using System.Text.Json;

namespace Fimple.Atm.Tests.Integration
{
    [Collection(ApiCollection.NAME)]
    public class HealthAndDocumentationTests : IntegrationTestBase
    {
        public HealthAndDocumentationTests(ApiFixture fixture) : base(fixture)
        {

        }

        [Fact]
        public async Task HealthEndpointReportsTheDatabaseCheck()
        {
            var response = await Api.Get("/health");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal("Healthy", document.RootElement.GetProperty("status").GetString());
            var checks = document.RootElement.GetProperty("checks").EnumerateArray().ToList();
            var databaseCheck = Assert.Single(checks);
            Assert.Equal("postgresql", databaseCheck.GetProperty("name").GetString());
            Assert.Equal("Healthy", databaseCheck.GetProperty("status").GetString());
        }

        [Fact]
        public async Task LivenessEndpointDoesNotNeedTheDatabase()
        {
            var response = await Api.Get("/health/live");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task OpenApiDocumentDescribesTheEndpointsHeadersAndEnumValues()
        {
            var response = await Api.Get("/openapi/v1.json");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var json = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(json);
            var paths = document.RootElement.GetProperty("paths");

            foreach (var expectedPath in new[]
            {
                "/api/atms",
                "/api/atms/{atmId}/cassettes",
                "/api/atms/{atmId}/withdrawals/quotes",
                "/api/atms/{atmId}/withdrawals/quotes/{quoteId}/confirm",
                "/api/atms/{atmId}/deposits",
                "/api/atms/{atmId}/reconciliations",
                "/api/atms/{atmId}/consistency",
                "/api/reports/operations"
            })
            {
                Assert.True(paths.TryGetProperty(expectedPath, out _), $"Missing path {expectedPath}");
            }

            Assert.Contains("\"Idempotency-Key\"", json);
            Assert.Contains("\"X-User\"", json);
            Assert.Contains("IN_SERVICE", json);
            Assert.Contains("RECONCILIATION_ADJUSTMENT", json);
            Assert.Equal("ATM Cash Management API", document.RootElement.GetProperty("info").GetProperty("title").GetString());
        }

        [Fact]
        public async Task SwaggerUiIsServedAndTheRootRedirectsToIt()
        {
            var swagger = await Api.Get("/swagger/index.html");
            var root = await Api.Get("/");

            Assert.Equal(HttpStatusCode.OK, swagger.StatusCode);
            Assert.Equal(HttpStatusCode.OK, root.StatusCode);
            Assert.Equal("text/html", root.Content.Headers.ContentType?.MediaType);
        }
    }
}
