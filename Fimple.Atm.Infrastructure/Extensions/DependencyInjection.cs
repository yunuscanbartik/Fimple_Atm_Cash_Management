using Fimple.Atm.Domain.Interfaces;
using Fimple.Atm.Infrastructure.PostgreSQL;
using Fimple.Atm.Infrastructure.Repositories;
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
            services.AddRepositories();
            return services;
        }

        private static IServiceCollection AddRepositories(this IServiceCollection services)
        {
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IAtmRepository, AtmRepository>();
            services.AddScoped<ICassetteMovementRepository, CassetteMovementRepository>();
            services.AddScoped<IWithdrawalQuoteRepository, WithdrawalQuoteRepository>();
            services.AddScoped<IDepositRepository, DepositRepository>();
            services.AddScoped<IReconciliationRepository, ReconciliationRepository>();
            services.AddScoped<IExchangeRateRepository, ExchangeRateRepository>();
            services.AddScoped<IOutboxRepository, OutboxRepository>();
            return services;
        }
    }
}
