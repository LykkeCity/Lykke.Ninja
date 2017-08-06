using Autofac;
using AzureStorage.Queue;
using Common;
using Common.Log;
using Lykke.Ninja.Core.AlertNotifications;
using Lykke.Ninja.Core.Block;
using Lykke.Ninja.Core.Ninja.Block;
using Lykke.Ninja.Core.Ninja.Transaction;
using Lykke.Ninja.Core.ParseBlockCommand;
using Lykke.Ninja.Core.Queue;
using Lykke.Ninja.Core.Settings;
using Lykke.Ninja.Core.Transaction;
using Lykke.AzureQueueIntegration.Publisher;
using Lykke.JobTriggers.Abstractions;
using Lykke.MonitoringServiceApiCaller;
using Lykke.SlackNotification.AzureQueue;
using Lykke.SlackNotifications;
using Microsoft.Extensions.PlatformAbstractions;
using QBitNinja.Client;
using Lykke.Ninja.Repositories.ServiceMonitoring;
using Lykke.Ninja.Services.AlertNotifications;
using Lykke.Ninja.Services.Block;
using Lykke.Ninja.Services.Ninja.Block;
using Lykke.Ninja.Services.Ninja.Transaction;
using Lykke.Ninja.Services.PaseBlockCommand;
using IMonitoringService = Lykke.Ninja.Core.ServiceMonitoring.IMonitoringService;

namespace Lykke.Ninja.Services
{
    public static class SrvBinder
    {
        public static void BindCommonServices(this ContainerBuilder ioc, GeneralSettings generalSettings, ILog log)
        {
            var settings = generalSettings.LykkeNinja;
            ioc.RegisterInstance(new MonitoringService(new MonitoringServiceFacade(generalSettings.MonitoringServiceClient.MonitoringServiceUrl)))
                .As<IMonitoringService>();
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
        }
    }
}
