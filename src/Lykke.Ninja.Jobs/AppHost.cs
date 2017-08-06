using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Loader;
using System.Text;
using System.Threading;
using Autofac.Extensions.DependencyInjection;
using Core.Settings;
using Core.Settings.Validation;
using Jobs.Binders;
using Lykke.JobTriggers.Triggers;
using Microsoft.Extensions.Configuration;

namespace Jobs
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

                var triggerHost = new TriggerHost(serviceProvider);

                var end = new ManualResetEvent(false);

                AssemblyLoadContext.Default.Unloading += ctx =>
                {
                    Console.WriteLine("SIGTERM recieved");
                    triggerHost.Cancel();

                    end.WaitOne();
                };

                triggerHost.Start().Wait();
                end.Set();
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
