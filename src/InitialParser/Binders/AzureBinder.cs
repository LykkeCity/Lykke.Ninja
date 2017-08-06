using System;
using Autofac;
using Autofac.Features.ResolveAnything;
using AzureStorage.Tables;
using Common;
using Common.Log;
using Lykke.Ninja.Core.Settings;
using Lykke.Ninja.Repositories;
using Lykke.Ninja.Repositories.Log;
using Lykke.Ninja.Services;

namespace InitialPerser.Binders
{
    public class AzureBinder
    {
        public ContainerBuilder Bind(GeneralSettings generalSettings)
        {
            var settings = generalSettings.LykkeNinja;
            var logToTable = new LogToTable(new AzureTableStorage<LogEntity>(settings.Db.LogsConnString, "LykkeNinjaInitialParserError", null),
                                            new AzureTableStorage<LogEntity>(settings.Db.LogsConnString, "LykkeNinjaInitialParserWarning", null),
                                            new AzureTableStorage<LogEntity>(settings.Db.LogsConnString, "LykkeNinjaInitialParserInfo", null));
            var log = new LogToTableAndConsole(logToTable, new LogToConsole());

            var ioc = new ContainerBuilder();

            var consoleWriter = new ConsoleLWriter(p =>
            {
                Console.WriteLine($"{DateTime.UtcNow:T} -  {p}");
            });

            ioc.RegisterInstance(consoleWriter).As<IConsole>();

            
            InitContainer(ioc, generalSettings, log);

            return ioc;
        }

        private void InitContainer(ContainerBuilder ioc, GeneralSettings generalSettings, ILog log)
        {
            var settings = generalSettings.LykkeNinja;
#if DEBUG
            log.WriteInfoAsync("Lykke.Ninja GrabNinjaDataFunctions", "App start", null, $"BaseSettings : {settings.ToJson()}").Wait();
#else
            log.WriteInfoAsync("Lykke.Ninja GrabNinjaDataFunctions", "App start", null, $"BaseSettings : private").Wait();
#endif

            ioc.RegisterInstance(log);
            ioc.RegisterInstance(settings);

            ioc.BindCommonServices(generalSettings, log);
            ioc.BindRepositories(settings, log);

            ioc.RegisterSource(new AnyConcreteTypeNotAlreadyRegisteredSource());
        }        
    }
}
