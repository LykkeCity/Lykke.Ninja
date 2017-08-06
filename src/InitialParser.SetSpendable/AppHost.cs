using System.IO;
using Autofac.Extensions.DependencyInjection;
using Common.Log;
using Core.Settings;
using Core.Settings.Validation;
using InitialParser.SetSpendable.Binders;
using InitialParser.SetSpendable.Functions;
using Microsoft.Extensions.Configuration;
using Services;

namespace InitialParser.SetSpendable
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

                var service = ((SetSpendableFunctions) serviceProvider.GetService(typeof(SetSpendableFunctions)));
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
