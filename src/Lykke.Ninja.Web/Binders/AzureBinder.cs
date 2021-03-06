﻿using System;
using Autofac;
using Autofac.Features.ResolveAnything;
using AzureStorage.Tables;
using Common;
using Common.Log;
using Lykke.Ninja.Core.Settings;
using Lykke.Ninja.Repositories;
using Lykke.Ninja.Services;

namespace Lykke.Ninja.Web.Binders
{
    public class AzureBinder
    {
        public ContainerBuilder Bind(GeneralSettings generalSettings, ILog log)
        {

            var ioc = new ContainerBuilder();

            var consoleWriter = new ConsoleLWriter(Console.WriteLine);

            ioc.RegisterInstance(consoleWriter).As<IConsole>();

            
            InitContainer(ioc, generalSettings, log);

            return ioc;
        }

        private void InitContainer(ContainerBuilder ioc, GeneralSettings generalSettings, ILog log)
        {
            var settings = generalSettings.LykkeNinja;
#if DEBUG
            log.WriteInfoAsync("Lykke.Ninja Lykke.Ninja.Web", "App start", null, $"BaseSettings : {settings.ToJson()}").Wait();
#else
            log.WriteInfoAsync("Lykke.Ninja Lykke.Ninja.Web", "App start", null, $"BaseSettings : private").Wait();
#endif

            ioc.RegisterInstance(log);
            ioc.RegisterInstance(settings);

            ioc.BindCommonServices(generalSettings, log);
            ioc.BindRepositories(settings, log);
        }        
    }
}
