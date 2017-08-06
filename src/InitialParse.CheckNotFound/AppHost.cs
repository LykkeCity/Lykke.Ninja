using System.IO;
using Autofac.Extensions.DependencyInjection;
using Common.Log;
using Lykke.Ninja.Core.Settings;
using Lykke.Ninja.Core.Settings.Validation;
using InitialParse.CheckNotFound.Binders;
using InitialParse.CheckNotFound.Functions;
using Microsoft.Extensions.Configuration;
using Lykke.Ninja.Services;

namespace InitialParse.CheckNotFound
{
    public class AppHost
    {
        public IConfigurationRoot Configuration { get; }

        public AppHost()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();

            Configuration = builder.Build();
        }

        public void Run()
        {
            {
                var settings = GetSettings();

                var appContainer = new AzureBinder().Bind(settings).Build();
                var serviceProvider = new AutofacServiceProvider(appContainer);

                var service = ((CheckNotFoundFunctions)serviceProvider.GetService(typeof(CheckNotFoundFunctions)));
                Retry.Try(() => service.Run(), logger: ((ILog)serviceProvider.GetService(typeof(ILog)))).Wait();
            }
        }

        private GeneralSettings GetSettings()
        {
#if DEBUG
            var settings = GeneralSettingsReader.ReadGeneralSettingsLocal<GeneralSettings>("../../settings.json");
#else
            var settings = GeneralSettingsReader.ReadGeneralSettings<GeneralSettings>(Configuration["SettingsUrl"]);
#endif

            GeneralSettingsValidator.Validate(settings);

            return settings;
        }
    }
}
