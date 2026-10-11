using System.Text.Json;
using System.Text.Json.Serialization;
using Fimple.Atm.Api.OpenApi;
using Microsoft.AspNetCore.Http.Json;

namespace Fimple.Atm.Api.Extensions
{
    public static class OpenApiExtensions
    {
        private const string ENABLED_KEY = "OpenApi:Enabled";
        private const string DOCUMENT_NAME = "v1";
        private const string DOCUMENT_URL = "/openapi/v1.json";
        private const string SWAGGER_ROUTE_PREFIX = "swagger";
        private const string SWAGGER_DOCUMENT_TITLE = "ATM Cash Management API v1";

        public static IServiceCollection AddAtmOpenApi(this IServiceCollection services)
        {
            services.Configure<JsonOptions>(options =>
            {
                options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseUpper));
            });

            services.AddOpenApi(DOCUMENT_NAME, options =>
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
            var isEnabled = app.Configuration.GetValue(ENABLED_KEY, true);
            if (!isEnabled) return app;

            app.MapOpenApi();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint(DOCUMENT_URL, SWAGGER_DOCUMENT_TITLE);
                options.RoutePrefix = SWAGGER_ROUTE_PREFIX;
                options.DocumentTitle = SWAGGER_DOCUMENT_TITLE;
            });
            app.MapGet("/", () => Results.Redirect($"/{SWAGGER_ROUTE_PREFIX}")).ExcludeFromDescription();

            return app;
        }
    }
}
