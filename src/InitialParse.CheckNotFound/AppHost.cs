using System.IO;
using Autofac.Extensions.DependencyInjection;
using Common.Log;
using Core.Settings;
using Core.Settings.Validation;
using InitialParse.CheckNotFound.Binders;
using InitialParse.CheckNotFound.Functions;
using Microsoft.Extensions.Configuration;
using Services;

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

        private BaseSettings GetSettings()
        {
#if DEBUG
            var settings = GeneralSettingsReader.ReadGeneralSettingsLocal<BaseSettings>("../../settings.json");
#else
            var generalSettings = GeneralSettingsReader.ReadGeneralSettings<GeneralSettings>(Configuration["SettingsUrl"]);
            var settings = generalSettings?.LykkeNinja;
#endif

            GeneralSettingsValidator.Validate(settings);

            return settings;
        }
    }
}
