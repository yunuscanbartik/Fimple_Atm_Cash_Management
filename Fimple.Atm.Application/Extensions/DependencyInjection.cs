using Fimple.Atm.Application.Services;
using Fimple.Atm.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Fimple.Atm.Application.Extensions
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.TryAddSingleton(TimeProvider.System);
            services.AddScoped<ICassetteMovementRecorder, CassetteMovementRecorder>();
            services.AddScoped<IAtmService, AtmService>();
            services.AddScoped<ICassetteService, CassetteService>();
            return services;
        }
    }
}
