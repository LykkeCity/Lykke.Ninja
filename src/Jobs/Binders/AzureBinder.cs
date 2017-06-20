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
        public ContainerBuilder Bind(BaseSettings settings)
        {
            var logToTable = new LogToTable(new AzureTableStorage<LogEntity>(settings.Db.LogsConnString, "LykkeNinjaJobsWebError", null),
                                            new AzureTableStorage<LogEntity>(settings.Db.LogsConnString, "LykkeNinjaJobsWebWarning", null),
                                            new AzureTableStorage<LogEntity>(settings.Db.LogsConnString, "LykkeNinjaJobsWebInfo", null));
            var log = new LogToTableAndConsole(logToTable, new LogToConsole());

            var ioc = new ContainerBuilder();

            var consoleWriter = new ConsoleLWriter(Console.WriteLine);

            ioc.RegisterInstance(consoleWriter).As<IConsole>();

            
            InitContainer(ioc, settings, log);

            return ioc;
        }

        private void InitContainer(ContainerBuilder ioc, BaseSettings settings, ILog log)
        {
#if DEBUG
            log.WriteInfoAsync("Lykke.Ninja Jobs", "App start", null, $"BaseSettings : {settings.ToJson()}").Wait();
#else
            log.WriteInfoAsync("Lykke.Ninja Jobs", "App start", null, $"BaseSettings : private").Wait();
#endif

            ioc.RegisterInstance(log);
            ioc.RegisterInstance(settings);

            ioc.BindCommonServices(settings, log);
            ioc.BindRepositories(settings, log);
            ioc.BindBackgroundJobs(settings, log);

            ioc.RegisterSource(new AnyConcreteTypeNotAlreadyRegisteredSource());
        }        
    }
}
