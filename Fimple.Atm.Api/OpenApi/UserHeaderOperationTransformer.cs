using Fimple.Atm.Api.Filters;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Fimple.Atm.Api.OpenApi
{
    public class UserHeaderOperationTransformer : IOpenApiOperationTransformer
    {
        private const string ReconciliationSegment = "reconciliations";
        private const string LoadSuffix = "/load";
        private const string UnloadSuffix = "/unload";
        private const string PostMethod = "POST";

        public Task TransformAsync(
            OpenApiOperation operation,
            OpenApiOperationTransformerContext operationTransformerContext,
            CancellationToken cancellationToken)
        {
            var path = operationTransformerContext.Description.RelativePath ?? string.Empty;
            var isPost = operationTransformerContext.Description.HttpMethod == PostMethod;
            var isReconciliationWrite = isPost && path.Contains(ReconciliationSegment);
            var isCashMovement = path.EndsWith(LoadSuffix) || path.EndsWith(UnloadSuffix);
            if (!isReconciliationWrite && !isCashMovement) return Task.CompletedTask;

            operation.Parameters ??= new List<IOpenApiParameter>();
            operation.Parameters.Add(new OpenApiParameter
            {
                Name = UserHeaderActionFilter.UserHeaderName,
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
