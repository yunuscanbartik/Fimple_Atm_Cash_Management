using Microsoft.Extensions.Hosting;

namespace Fimple.Atm.Infrastructure.Extensions
{
    public static class HostApplicationBuilderExtensions
    {
        public static IHostApplicationBuilder AddLocalSettingsInDevelopment(this IHostApplicationBuilder hostApplicationBuilder)
        {
            if (hostApplicationBuilder.Environment.IsDevelopment())
            {
                hostApplicationBuilder.Configuration.AddLocalSettings(hostApplicationBuilder.Environment.ContentRootPath);
            }

            return hostApplicationBuilder;
        }
    }
}
