﻿using System;
using Autofac;
using Autofac.Features.ResolveAnything;
using AzureStorage.Tables;
using Common;
using Common.Log;
using Lykke.Ninja.Core.Settings;
using Lykke.Ninja.Repositories;
using Lykke.Ninja.Services;

namespace InitialParse.CheckNotFound.Binders
{
    public class AzureBinder
    {
        public ContainerBuilder Bind(GeneralSettings generalSettings)
        {
            throw new NotImplementedException();
            //var settings = generalSettings.LykkeNinja;
            //var logToTable = new LogToTable(new AzureTableStorage<LogEntity>(settings.Db.LogsConnString, "LykkeNinjaInitialParserCheckNotFoundError", null),
            //                                new AzureTableStorage<LogEntity>(settings.Db.LogsConnString, "LykkeNinjaInitialParserCheckNotFoundWarning", null),
            //                                new AzureTableStorage<LogEntity>(settings.Db.LogsConnString, "LykkeNinjaInitialParserCheckNotFoundInfo", null));
            //var log = new LogToTableAndConsole(logToTable, new LogToConsole());

            //var ioc = new ContainerBuilder();

            //var consoleWriter = new ConsoleLWriter(p =>
            //{
            //    Console.WriteLine($"{DateTime.UtcNow:T} -  {p}");
            //});

            //ioc.RegisterInstance(consoleWriter).As<IConsole>();

            
            //InitContainer(ioc, generalSettings, log);

            //return ioc;
        }

        private void InitContainer(ContainerBuilder ioc, GeneralSettings generalSettings, ILog log)
        {
            var settings = generalSettings.LykkeNinja;
#if DEBUG
            log.WriteInfoAsync("Lykke.Ninja InitialParserCheckNotFoundFunctions", "App start", null, $"BaseSettings : {settings.ToJson()}").Wait();
#else
            log.WriteInfoAsync("Lykke.Ninja InitialParserCheckNotFoundFunctions", "App start", null, $"BaseSettings : private").Wait();
#endif

            ioc.RegisterInstance(log);
            ioc.RegisterInstance(settings);

            ioc.BindCommonServices(generalSettings, log);
            ioc.BindRepositories(settings, log);

            ioc.RegisterSource(new AnyConcreteTypeNotAlreadyRegisteredSource());
        }        
    }
}
