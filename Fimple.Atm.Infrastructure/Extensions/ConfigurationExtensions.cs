using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;

namespace Fimple.Atm.Infrastructure.Extensions
{
    public static class ConfigurationExtensions
    {
        private const string LOCAL_SETTINGS_FILE_NAME = "appsettings.Local.json";

        public static IConfigurationBuilder AddLocalSettings(this IConfigurationBuilder configurationBuilder, string startDirectory)
        {
            var directory = new DirectoryInfo(startDirectory);
            while (directory != null)
            {
                if (File.Exists(Path.Combine(directory.FullName, LOCAL_SETTINGS_FILE_NAME)))
                {
                    var fileProvider = new PhysicalFileProvider(directory.FullName);
                    configurationBuilder.AddJsonFile(fileProvider, LOCAL_SETTINGS_FILE_NAME, optional: false, reloadOnChange: false);
                    configurationBuilder.AddEnvironmentVariables();
                    return configurationBuilder;
                }

                directory = directory.Parent;
            }

            return configurationBuilder;
        }
    }
}
