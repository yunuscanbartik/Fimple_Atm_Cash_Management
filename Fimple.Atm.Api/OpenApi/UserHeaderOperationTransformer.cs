using Fimple.Atm.Api.Filters;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Fimple.Atm.Api.OpenApi
{
    public class UserHeaderOperationTransformer : IOpenApiOperationTransformer
    {
        private const string RECONCILIATION_SEGMENT = "reconciliations";
        private const string LOAD_SUFFIX = "/load";
        private const string UNLOAD_SUFFIX = "/unload";
        private const string POST_METHOD = "POST";

        public Task TransformAsync(
            OpenApiOperation operation,
            OpenApiOperationTransformerContext operationTransformerContext,
            CancellationToken cancellationToken)
        {
            var path = operationTransformerContext.Description.RelativePath ?? string.Empty;
            var isPost = operationTransformerContext.Description.HttpMethod == POST_METHOD;
            var isReconciliationWrite = isPost && path.Contains(RECONCILIATION_SEGMENT);
            var isCashMovement = path.EndsWith(LOAD_SUFFIX) || path.EndsWith(UNLOAD_SUFFIX);
            if (!isReconciliationWrite && !isCashMovement) return Task.CompletedTask;

            operation.Parameters ??= new List<IOpenApiParameter>();
            operation.Parameters.Add(new OpenApiParameter
            {
                Name = UserHeaderActionFilter.USER_HEADER_NAME,
                In = ParameterLocation.Header,
                Required = isReconciliationWrite,
                Description = isReconciliationWrite
                    ? "Name of the operations officer. Required; the approver of a reconciliation must differ from the user who entered it."
                    : "Name of the operations officer, recorded on the cassette movements.",
                Schema = new OpenApiSchema { Type = JsonSchemaType.String }
            });

            return Task.CompletedTask;
        }
    }
}
