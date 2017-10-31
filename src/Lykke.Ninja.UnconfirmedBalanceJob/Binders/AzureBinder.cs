using System;
using Autofac;
using Autofac.Features.ResolveAnything;
using AzureStorage.Tables;
using Common;
using Common.Log;
using Lykke.Ninja.Core.Settings;
using Lykke.Ninja.Repositories;
using Lykke.Ninja.Services;

namespace Lykke.Ninja.UnconfirmedBalanceJob.Binders
{
    public class AzureBinder
    {
        public ContainerBuilder Bind(GeneralSettings settings, ILog log)
        {

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
            log.WriteInfoAsync("Lykke.Ninja Lykke.Ninja.UnconfirmedBalanceJobs", "App start", null, $"BaseSettings : {settings.ToJson()}").Wait();
#else
            log.WriteInfoAsync("Lykke.Ninja Lykke.Ninja.UnconfirmedBalanceJobs", "App start", null, $"BaseSettings : private").Wait();
#endif

            ioc.RegisterInstance(log);
            ioc.RegisterInstance(settings.LykkeNinja);

            ioc.BindCommonServices(settings, log);
            ioc.BindRepositories(settings.LykkeNinja, log);
            ioc.BindBackgroundJobs(settings.LykkeNinja, log);
        }        
    }
}
