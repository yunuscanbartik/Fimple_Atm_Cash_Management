using Fimple.Atm.Infrastructure.PostgreSQL;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Fimple.Atm.Infrastructure.Extensions
{
    public static class DependencyInjection
    {
        private const string CONNECTION_STRING_MISSING_MESSAGE = "PostgreSQL connection string is missing.";
        private const string DATABASE_HEALTH_CHECK_NAME = "postgresql";

        public static IServiceCollection AddPostgresDatabase(
            this IServiceCollection services,
            string? postgresConnectionString)
        {
            if (string.IsNullOrWhiteSpace(postgresConnectionString))
            {
                throw new InvalidOperationException(CONNECTION_STRING_MISSING_MESSAGE);
            }

            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(postgresConnectionString));
            services.AddHealthChecks().AddDbContextCheck<AppDbContext>(DATABASE_HEALTH_CHECK_NAME);
            return services;
        }
    }
}
