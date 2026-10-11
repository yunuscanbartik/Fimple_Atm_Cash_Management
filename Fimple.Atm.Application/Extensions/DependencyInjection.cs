using Fimple.Atm.Application.Services;
using Fimple.Atm.Domain.Configs;
using Fimple.Atm.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Fimple.Atm.Application.Extensions
{
    public static class DependencyInjection
    {
        private const string CARD_HASHING_KEY_INVALID_MESSAGE = "CardHashing:Key must be configured with at least 32 characters.";

        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions<CardHashingConfig>()
                .Bind(configuration.GetSection(CardHashingConfig.SECTION_NAME))
                .Validate(IsCardHashingKeyValid, CARD_HASHING_KEY_INVALID_MESSAGE)
                .ValidateOnStart();

            services.TryAddSingleton(TimeProvider.System);
            services.AddSingleton<ICardHasher, CardHasher>();
            services.AddScoped<ICassetteMovementRecorder, CassetteMovementRecorder>();
            services.AddScoped<IQuoteReservationReleaser, QuoteReservationReleaser>();
            services.AddScoped<IQuoteExpiryService, QuoteExpiryService>();
            services.AddScoped<IAtmService, AtmService>();
            services.AddScoped<ICassetteService, CassetteService>();
            services.AddScoped<IWithdrawalService, WithdrawalService>();
            services.AddScoped<IDepositService, DepositService>();
            services.AddScoped<IReconciliationService, ReconciliationService>();
            services.AddScoped<IConsistencyService, ConsistencyService>();
            services.AddScoped<IReportService, ReportService>();
            services.AddScoped<IOutboxProcessor, OutboxProcessor>();
            return services;
        }

        private static bool IsCardHashingKeyValid(CardHashingConfig cardHashingConfig)
        {
            return cardHashingConfig.Key.Length >= CardHashingConfig.MINIMUM_KEY_LENGTH;
        }
    }
}
