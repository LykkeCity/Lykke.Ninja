using System;
using Autofac;
using Autofac.Features.ResolveAnything;
using AzureStorage.Tables;
using Common;
using Common.Log;
using Core.Settings;
using Repositories;
using Repositories.Log;
using Services;

namespace Jobs.Binders
{
    public class AzureBinder
    {
        public ContainerBuilder Bind(GeneralSettings settings)
        {
            var logToTable = new LogToTable(new AzureTableStorage<LogEntity>(settings.LykkeNinja.Db.LogsConnString, "LykkeNinjaJobsError", null),
                                            new AzureTableStorage<LogEntity>(settings.LykkeNinja.Db.LogsConnString, "LykkeNinjaJobsWarning", null),
                                            new AzureTableStorage<LogEntity>(settings.LykkeNinja.Db.LogsConnString, "LykkeNinjaJobsInfo", null));
            var log = new LogToTableAndConsole(logToTable, new LogToConsole());

            var ioc = new ContainerBuilder();

            var consoleWriter = new ConsoleLWriter(p =>
            {
                Console.WriteLine($"{DateTime.UtcNow:T} -  {p}");
            });

            ioc.RegisterInstance(consoleWriter).As<IConsole>();

            
            InitContainer(ioc, settings, log);

            return ioc;
        }

        private void InitContainer(ContainerBuilder ioc, GeneralSettings settings, ILog log)
        {
#if DEBUG
            log.WriteInfoAsync("Lykke.Ninja Jobs", "App start", null, $"BaseSettings : {settings.ToJson()}").Wait();
#else
            log.WriteInfoAsync("Lykke.Ninja Jobs", "App start", null, $"BaseSettings : private").Wait();
#endif

            ioc.RegisterInstance(log);
            ioc.RegisterInstance(settings.LykkeNinja);

            ioc.BindCommonServices(settings, log);
            ioc.BindRepositories(settings.LykkeNinja, log);
            ioc.BindBackgroundJobs(settings.LykkeNinja, log);

            ioc.RegisterSource(new AnyConcreteTypeNotAlreadyRegisteredSource());
        }        
    }
}
