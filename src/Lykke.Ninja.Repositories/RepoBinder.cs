using Autofac;
using AzureStorage.Queue;
using AzureStorage.Tables;
using Common.Log;
using Lykke.Ninja.Core.AlertNotifications;
using Lykke.Ninja.Core.BlockStatus;
using Lykke.Ninja.Core.ParseBlockCommand;
using Lykke.Ninja.Core.Queue;
using Lykke.Ninja.Core.ServiceMonitoring;
using Lykke.Ninja.Core.Settings;
using Lykke.Ninja.Core.Transaction;
using Lykke.Ninja.Repositories.BlockStatuses;
using Lykke.Ninja.Repositories.Mongo;
using Lykke.Ninja.Repositories.ParseBlockCommand;
using Lykke.Ninja.Repositories.Transactions;

namespace Lykke.Ninja.Repositories
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




            ioc.Register(p => new ParseBlockCommandProducer(new AzureQueueExt(settings.Db.DataConnString, QueueNames.ParseBlockTasks)))
                .As<IParseBlockCommandProducer>();

            ioc.Register(p => new FixAddressCommandProducer(new AzureQueueExt(settings.Db.DataConnString, QueueNames.AddressesToFix)))
                .As<IFixAddressCommandProducer>();
            
        }
    }
}
