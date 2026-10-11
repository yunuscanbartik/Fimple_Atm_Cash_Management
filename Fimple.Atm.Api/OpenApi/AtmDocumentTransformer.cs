using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Fimple.Atm.Api.OpenApi
{
    public class AtmDocumentTransformer : IOpenApiDocumentTransformer
    {
        private const string Title = "ATM Cash Management API";
        private const string Version = "v1";
        private const string Description =
            "Cash management backend for a bank's ATM network: ATM lifecycle, cassette configuration, two-step withdrawals " +
            "(quote and confirm with an idempotency key), deposits, reconciliation with four-eyes approval, a movement ledger " +
            "with a consistency check, and an operations report. Errors are returned as RFC 7807 problem details with an errorCode.";

        public Task TransformAsync(
            OpenApiDocument openApiDocument,
            OpenApiDocumentTransformerContext openApiDocumentTransformerContext,
            CancellationToken cancellationToken)
        {
            openApiDocument.Info = new OpenApiInfo
            {
                Title = Title,
                Version = Version,
                Description = Description
            };

            return Task.CompletedTask;
        }
    }
}
