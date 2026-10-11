using Fimple.Atm.Infrastructure.Extensions;
using Fimple.Atm.Infrastructure.PostgreSQL;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using Npgsql;

namespace Fimple.Atm.Tests.Integration
{
    public class ApiFixture : IAsyncLifetime
    {
        private const string TEST_DATABASE_NAME = "fimple_atm_test";
        private const string ADMIN_DATABASE_NAME = "postgres";
        private const string TEST_CONNECTION_STRING_VARIABLE = "FIMPLE_TEST_CONNECTION_STRING";
        private const string CONNECTION_STRING_KEY = "ConnectionStrings:PostgreSql";
        private const string CONNECTION_STRING_ENVIRONMENT_KEY = "ConnectionStrings__PostgreSql";

        private WebApplicationFactory<Program>? _factory;
        private ApiClient? _apiClient;

        public FakeTimeProvider Clock { get; } = new(DateTimeOffset.UtcNow);

        public ApiClient Api => _apiClient ?? throw new InvalidOperationException("The fixture has not been initialized.");

        public IServiceProvider Services => _factory?.Services ?? throw new InvalidOperationException("The fixture has not been initialized.");

        public async Task InitializeAsync()
        {
            var baseConnectionString = ResolveBaseConnectionString();
            await RecreateTestDatabase(baseConnectionString);

            var testConnectionString = WithDatabase(baseConnectionString, TEST_DATABASE_NAME);
            Environment.SetEnvironmentVariable(CONNECTION_STRING_ENVIRONMENT_KEY, testConnectionString);

            _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<TimeProvider>();
                    services.AddSingleton<TimeProvider>(Clock);
                });
            });
            _apiClient = new ApiClient(_factory.CreateClient());
        }

        public async Task DisposeAsync()
        {
            if (_factory != null)
            {
                await _factory.DisposeAsync();
            }

            Environment.SetEnvironmentVariable(CONNECTION_STRING_ENVIRONMENT_KEY, null);
            NpgsqlConnection.ClearAllPools();
            await DropTestDatabase(ResolveBaseConnectionString());
        }

        public async Task<T> WithDbContext<T>(Func<AppDbContext, Task<T>> action)
        {
            using var scope = Services.CreateScope();
            var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            return await action(appDbContext);
        }

        public async Task<TResult> WithScopedService<TService, TResult>(Func<TService, Task<TResult>> action) where TService : notnull
        {
            using var scope = Services.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<TService>();
            return await action(service);
        }

        private static string ResolveBaseConnectionString()
        {
            var configuration = new ConfigurationBuilder()
                .AddUserSecrets(typeof(Program).Assembly)
                .AddLocalSettings(AppContext.BaseDirectory)
                .AddEnvironmentVariables()
                .Build();

            var connectionString = configuration[TEST_CONNECTION_STRING_VARIABLE] ?? configuration[CONNECTION_STRING_KEY];
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    $"No PostgreSQL connection string for the integration tests. Set the {TEST_CONNECTION_STRING_VARIABLE} environment variable " +
                    $"or create appsettings.Local.json in the repository root (see appsettings.Local.example.json).");
            }

            return connectionString;
        }

        private static string WithDatabase(string connectionString, string databaseName)
        {
            var builder = new NpgsqlConnectionStringBuilder(connectionString) { Database = databaseName };
            return builder.ConnectionString;
        }

        private static async Task RecreateTestDatabase(string baseConnectionString)
        {
            await using var connection = new NpgsqlConnection(WithDatabase(baseConnectionString, ADMIN_DATABASE_NAME));
            await connection.OpenAsync();
            await ExecuteAsync(connection, $"DROP DATABASE IF EXISTS {TEST_DATABASE_NAME} WITH (FORCE)");
            await ExecuteAsync(connection, $"CREATE DATABASE {TEST_DATABASE_NAME}");
        }

        private static async Task DropTestDatabase(string baseConnectionString)
        {
            await using var connection = new NpgsqlConnection(WithDatabase(baseConnectionString, ADMIN_DATABASE_NAME));
            await connection.OpenAsync();
            await ExecuteAsync(connection, $"DROP DATABASE IF EXISTS {TEST_DATABASE_NAME} WITH (FORCE)");
        }

        private static async Task ExecuteAsync(NpgsqlConnection connection, string commandText)
        {
            await using var command = new NpgsqlCommand(commandText, connection);
            await command.ExecuteNonQueryAsync();
        }
    }

    [CollectionDefinition(NAME)]
    public class ApiCollection : ICollectionFixture<ApiFixture>
    {
        public const string NAME = "Api";
    }
}
