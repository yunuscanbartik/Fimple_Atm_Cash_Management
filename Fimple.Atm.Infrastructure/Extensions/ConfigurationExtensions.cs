using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;

namespace Fimple.Atm.Infrastructure.Extensions
{
    public static class ConfigurationExtensions
    {
        private const string LocalSettingsFileName = "appsettings.Local.json";

        public static IConfigurationBuilder AddLocalSettings(this IConfigurationBuilder configurationBuilder, string startDirectory)
        {
            var directory = new DirectoryInfo(startDirectory);
            while (directory != null)
            {
                if (File.Exists(Path.Combine(directory.FullName, LocalSettingsFileName)))
                {
                    var fileProvider = new PhysicalFileProvider(directory.FullName);
                    configurationBuilder.AddJsonFile(fileProvider, LocalSettingsFileName, optional: false, reloadOnChange: false);
                    configurationBuilder.AddEnvironmentVariables();
                    return configurationBuilder;
                }

                directory = directory.Parent;
            }

            return configurationBuilder;
        }
    }
}
