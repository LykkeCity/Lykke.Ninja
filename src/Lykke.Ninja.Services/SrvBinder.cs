using System;
using System.Net;
using Autofac;
using Common.Log;
using Lykke.Ninja.Core.AlertNotifications;
using Lykke.Ninja.Core.Block;
using Lykke.Ninja.Core.Ninja.Block;
using Lykke.Ninja.Core.Ninja.Transaction;
using Lykke.Ninja.Core.ParseBlockCommand;
using Lykke.Ninja.Core.Settings;
using Lykke.AzureQueueIntegration.Publisher;
using Lykke.JobTriggers.Abstractions;
using Lykke.MonitoringServiceApiCaller;
using Lykke.Ninja.Core.Bitcoin;
using Lykke.Ninja.Core.UnconfirmedBalances.BalanceChanges;
using Lykke.Ninja.Core.UnconfirmedBalances.Statuses;
using Lykke.SlackNotification.AzureQueue;
using Lykke.SlackNotifications;
using Microsoft.Extensions.PlatformAbstractions;
using QBitNinja.Client;
using Lykke.Ninja.Services.AlertNotifications;
using Lykke.Ninja.Services.Bitcoin;
using Lykke.Ninja.Services.Block;
using Lykke.Ninja.Services.Ninja.Block;
using Lykke.Ninja.Services.Ninja.Transaction;
using Lykke.Ninja.Services.PaseBlockCommand;
using Lykke.Ninja.Services.UnconfirmedTransactions.BalanceChanges;
using Lykke.Ninja.Services.UnconfirmedTransactions.Statuses;
using NBitcoin;
using NBitcoin.RPC;

namespace Lykke.Ninja.Services
{
    public static class SrvBinder
    {
        public static void BindCommonServices(this ContainerBuilder ioc, GeneralSettings generalSettings, ILog log)
        {
            var settings = generalSettings.LykkeNinja;
            ioc.RegisterInstance(new QBitNinjaClient(settings.NinjaUrl, settings.UsedNetwork()) { Colored = true}).As<QBitNinjaClient>();
            ioc.RegisterType<NinjaBlockService>().As<INinjaBlockService>();
            ioc.RegisterType<NinjaTransactionService>().As<INinjaTransactionService>(); 
            ioc.RegisterType<ParseBlockCommandsService>().As<IParseBlockCommandsService>();
            ioc.RegisterType<BlockService>().As<IBlockService>();
            ioc.RegisterType<ProcessParseBlockCommandFacade>().As<IProcessParseBlockCommandFacade>();

            var slackSettings = new Lykke.AzureQueueIntegration.AzureQueueSettings
            {
                ConnectionString = generalSettings.SlackNotifications.AzureQueue.ConnectionString,
                QueueName = generalSettings.SlackNotifications.AzureQueue.QueueName
            };

            var slackClient = new SlackNotificationsSender(new AzureQueuePublisher<SlackMessageQueueEntity>(PlatformServices.Default.Application.ApplicationName, slackSettings).SetLogger(log).SetSerializer(new SlackNotificationsSerializer()).Start());
            
            ioc.Register(p => new SlackNotificationsProducer(slackClient))
                .As<ISlackNotificationsProducer>();
            
            ioc.Register(p => new SlackNotificationsProducer(slackClient))
                .As<IPoisionQueueNotifier>();


            ioc.RegisterInstance(new RPCClient(new NetworkCredential(settings.BitcoinRpc.Username, settings.BitcoinRpc.Password), settings.BitcoinRpc.IpAddress, settings.UsedNetwork()))
                .AsSelf();


            ioc.RegisterInstance(settings.UsedNetwork()).AsSelf();
            ioc.RegisterType<BitcoinRpcClient>().As<IBitcoinRpcClient>();
            ioc.RegisterType<UnconfirmedStatusesSinchronizeService>().As<IUnconfirmedStatusesSinchronizeService>();
            ioc.RegisterType<UnconfirmedBalanceChangesSinchronizeService>().As<IUnconfirmedBalanceChangesSinchronizeService>();
            
        }
    }
}
