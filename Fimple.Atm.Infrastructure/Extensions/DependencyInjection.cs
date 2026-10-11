using Fimple.Atm.Infrastructure.PostgreSQL;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Fimple.Atm.Infrastructure.Extensions
{
    public static class DependencyInjection
    {
        private const string ConnectionStringMissingMessage = "PostgreSQL connection string is missing.";

        public static IServiceCollection AddPostgresDatabase(
            this IServiceCollection services,
            string? postgresConnectionString)
        {
            if (string.IsNullOrWhiteSpace(postgresConnectionString))
            {
                throw new InvalidOperationException(ConnectionStringMissingMessage);
            }

            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(postgresConnectionString));
            return services;
        }
    }
}
