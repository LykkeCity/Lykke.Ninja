using Autofac;
using AzureStorage.Queue;
using Common;
using Common.Log;
using Core.AlertNotifications;
using Core.Block;
using Core.Ninja.Block;
using Core.Ninja.Transaction;
using Core.ParseBlockCommand;
using Core.Queue;
using Core.Settings;
using Core.Transaction;
using Lykke.AzureQueueIntegration.Publisher;
using Lykke.JobTriggers.Abstractions;
using Lykke.MonitoringServiceApiCaller;
using Lykke.SlackNotification.AzureQueue;
using Lykke.SlackNotifications;
using Microsoft.Extensions.PlatformAbstractions;
using QBitNinja.Client;
using Repositories.ServiceMonitoring;
using Services.AlertNotifications;
using Services.Block;
using Services.Ninja.Block;
using Services.Ninja.Transaction;
using Services.PaseBlockCommand;
using IMonitoringService = Core.ServiceMonitoring.IMonitoringService;

namespace Services
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
