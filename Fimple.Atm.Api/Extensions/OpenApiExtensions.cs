using System.Text.Json;
using System.Text.Json.Serialization;
using Fimple.Atm.Api.OpenApi;
using Microsoft.AspNetCore.Http.Json;

namespace Fimple.Atm.Api.Extensions
{
    public static class OpenApiExtensions
    {
        private const string EnabledKey = "OpenApi:Enabled";
        private const string DocumentName = "v1";
        private const string DocumentUrl = "/openapi/v1.json";
        private const string SwaggerRoutePrefix = "swagger";
        private const string SwaggerDocumentTitle = "ATM Cash Management API v1";

        public static IServiceCollection AddAtmOpenApi(this IServiceCollection services)
        {
            services.Configure<JsonOptions>(options =>
            {
                options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseUpper));
            });

            services.AddOpenApi(DocumentName, options =>
            {
                options.AddDocumentTransformer<AtmDocumentTransformer>();
                options.AddOperationTransformer<UserHeaderOperationTransformer>();
                options.CreateSchemaReferenceId = typeInfo =>
                {
                    var fullName = typeInfo.Type.FullName ?? typeInfo.Type.Name;
                    return fullName.Replace('.', '_').Replace('+', '_');
                };
            });

            return services;
        }

        public static WebApplication UseAtmOpenApi(this WebApplication app)
        {
            var isEnabled = app.Configuration.GetValue(EnabledKey, true);
            if (!isEnabled) return app;

            app.MapOpenApi();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint(DocumentUrl, SwaggerDocumentTitle);
                options.RoutePrefix = SwaggerRoutePrefix;
                options.DocumentTitle = SwaggerDocumentTitle;
            });
            app.MapGet("/", () => Results.Redirect($"/{SwaggerRoutePrefix}")).ExcludeFromDescription();

            return app;
        }
    }
}
