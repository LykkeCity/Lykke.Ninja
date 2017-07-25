using Autofac;
using AzureStorage.Queue;
using AzureStorage.Tables;
using Common.Log;
using Core.AlertNotifications;
using Core.BlockStatus;
using Core.ParseBlockCommand;
using Core.Queue;
using Core.ServiceMonitoring;
using Core.Settings;
using Core.Transaction;
using Lykke.JobTriggers.Abstractions;
using Repositories.AlertNotifications;
using Repositories.BlockStatuses;
using Repositories.Mongo;
using Repositories.ParseBlockCommand;
using Repositories.ServiceMonitoring;
using Repositories.Transactions;

namespace Repositories
{
    public static class RepoBinder
    {
        public static void BindRepositories(this ContainerBuilder ioc, BaseSettings settings, ILog log)
        {
            ioc.BindRepo(settings, log);
            ioc.BindQueue(settings);
        }

        private static void BindRepo(this ContainerBuilder ioc, BaseSettings settings, ILog log)
        {
            ioc.RegisterInstance(new ServiceMonitoringRepository(new AzureTableStorage<MonitoringRecordEntity>(settings.Db.SharedConnString, "Monitoring", log)))
                .As<IServiceMonitoringRepository>();

            ioc.RegisterInstance(new MongoSettings
            {
                ConnectionString = settings.NinjaData.ConnectionString,
                DataDbName = settings.NinjaData.DbName
            });

            ioc.RegisterType<BlockStatusesRepository>().As<IBlockStatusesRepository>().SingleInstance();
            ioc.RegisterType<TransactionOutputRepository>().As<ITransactionOutputRepository>().SingleInstance();
            ioc.RegisterType<TransactionInputRepository>().As<ITransactionInputRepository>().SingleInstance();
        }

        private static void BindQueue(this ContainerBuilder ioc, BaseSettings settings)
        {

            ioc.Register(p => new SlackNotificationsProducer(
                    new AzureQueueExt(settings.Db.SharedConnString, QueueNames.SlackNotifications)))
                .As<ISlackNotificationsProducer>();

            ioc.Register(p => new SlackNotificationsProducer(
                    new AzureQueueExt(settings.Db.SharedConnString, QueueNames.SlackNotifications)))
                .As<IPoisionQueueNotifier>();


            ioc.Register(p => new ParseBlockCommandProducer(new AzureQueueExt(settings.Db.DataConnString, QueueNames.ParseBlockTasks)))
                .As<IParseBlockCommandProducer>();

            ioc.Register(p => new FixAddressCommandProducer(new AzureQueueExt(settings.Db.DataConnString, QueueNames.AddressesToFix)))
                .As<IFixAddressCommandProducer>();
            
        }
    }
}
