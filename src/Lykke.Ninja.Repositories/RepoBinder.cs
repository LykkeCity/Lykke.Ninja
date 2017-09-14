using Autofac;
using AzureStorage.Queue;
using Common.Log;
using Lykke.Ninja.Core.AssetStats;
using Lykke.Ninja.Core.BlockStatus;
using Lykke.Ninja.Core.ParseBlockCommand;
using Lykke.Ninja.Core.Queue;
using Lykke.Ninja.Core.Settings;
using Lykke.Ninja.Core.Transaction;
using Lykke.Ninja.Core.UnconfirmedBalances.Statuses;
using Lykke.Ninja.Repositories.BlockStatuses;
using Lykke.Ninja.Repositories.ParseBlockCommand;
using Lykke.Ninja.Repositories.Transactions;
using Lykke.Ninja.Repositories.UnconfirmedBalances;

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
            ioc.RegisterType<BlockStatusesRepository>().As<IBlockStatusesRepository>().SingleInstance();
            ioc.RegisterType<TransactionOutputRepository>().As<ITransactionOutputRepository>().SingleInstance();
            ioc.RegisterType<TransactionOutputRepository>().As<IAssetStatsService>().SingleInstance();
            ioc.RegisterType<TransactionInputRepository>().As<ITransactionInputRepository>().SingleInstance();
            ioc.RegisterType<UnconfirmedTransactionStatusesRepository>().As<IUnconfirmedTransactionStatusesRepository>().SingleInstance();
        }

        private static void BindQueue(this ContainerBuilder ioc, BaseSettings settings)
        {
            ioc.Register(p => new ParseBlockCommandProducer(new AzureQueueExt(settings.Db.DataConnString, QueueNames.ParseBlockTasks)))
                .As<IParseBlockCommandProducer>();

            ioc.Register(p => new FixAddressCommandProducer(new AzureQueueExt(settings.Db.DataConnString, QueueNames.AddressesToFix)))
                .As<IFixAddressCommandProducer>();

            ioc.Register(p => new ScanNotFoundsCommandProducer(new AzureQueueExt(settings.Db.DataConnString, QueueNames.ScanNotFounds)))
                .As<IScanNotFoundsCommandProducer>();
        }
    }
}
