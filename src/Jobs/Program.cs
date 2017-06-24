using System;
using System.Runtime.Loader;
using Autofac.Extensions.DependencyInjection;
using Core.Settings;
using Core.Settings.Validation;
using Jobs.Binders;
using Lykke.JobTriggers.Triggers;
using System.Threading;

namespace Jobs
{
    class Program
    {
        static void Main(string[] args)
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

        private static BaseSettings GetSettings()
        {
#if DEBUG
            var settings = GeneralSettingsReader.ReadGeneralSettingsLocal<BaseSettings>("../../settings.json");
#else
            var generalSettings = GeneralSettingsReader.ReadGeneralSettings<GeneralSettings>(Configuration["SettingsUrl"]);
            var settings = generalSettings?.BcnReports;
#endif

            GeneralSettingsValidator.Validate(settings);

            return settings;
        }
    }
}