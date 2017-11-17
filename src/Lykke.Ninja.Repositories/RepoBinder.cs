using Autofac;
using AzureStorage.Queue;
using Common.Log;
using Lykke.Ninja.Core.AssetStats;
using Lykke.Ninja.Core.BlockStatus;
using Lykke.Ninja.Core.ParseBlockCommand;
using Lykke.Ninja.Core.Queue;
using Lykke.Ninja.Core.Settings;
using Lykke.Ninja.Core.Transaction;
using Lykke.Ninja.Core.UnconfirmedBalances.BalanceChanges;
using Lykke.Ninja.Core.UnconfirmedBalances.Statuses;
using Lykke.Ninja.Repositories.BlockStatuses;
using Lykke.Ninja.Repositories.ParseBlockCommand;
using Lykke.Ninja.Repositories.Transactions;
using Lykke.Ninja.Repositories.UnconfirmedBalances;
using MongoDB.Driver;

namespace Lykke.Ninja.Repositories
{
    public static class RepoBinder
    {
        public static void BindRepositories(this ContainerBuilder ioc, BaseSettings settings, ILog log)
        {
            ioc.BindMongoCollections(settings);
            ioc.BindRepo(settings, log);
            ioc.BindQueue(settings);
        }

        private static void BindRepo(this ContainerBuilder ioc, BaseSettings settings, ILog log)
        {
            ioc.RegisterType<BlockStatusesRepository>().As<IBlockStatusesRepository>().SingleInstance();
            ioc.RegisterType<TransactionOutputRepository>().As<ITransactionOutputRepository>().SingleInstance();
            ioc.RegisterType<TransactionOutputRepository>().As<IAssetStatsService>().SingleInstance();
            ioc.RegisterType<TransactionInputRepository>().As<ITransactionInputRepository>().SingleInstance();
            ioc.RegisterType<UnconfirmedStatusesRepository>().As<IUnconfirmedStatusesRepository>().SingleInstance();
            ioc.RegisterType<UnconfirmedBalanceChangesRepository>().As<IUnconfirmedBalanceChangesRepository>()
                .SingleInstance();
        }

        private static void BindQueue(this ContainerBuilder ioc, BaseSettings settings)
        {
            ioc.Register(p =>
                    new ParseBlockCommandProducer(new AzureQueueExt(settings.Db.DataConnString,
                        QueueNames.ParseBlockTasks)))
                .As<IParseBlockCommandProducer>();

            ioc.Register(p =>
                    new FixAddressCommandProducer(new AzureQueueExt(settings.Db.DataConnString,
                        QueueNames.AddressesToFix)))
                .As<IFixAddressCommandProducer>();

            ioc.Register(p =>
                    new ScanNotFoundsCommandProducer(new AzureQueueExt(settings.Db.DataConnString,
                        QueueNames.ScanNotFounds)))
                .As<IScanNotFoundsCommandProducer>();
        }

        private static void BindMongoCollections(this ContainerBuilder ioc, BaseSettings settings)
        {
            ioc.RegisterInstance(GetBlockStatusesCollection(settings)).As<IMongoCollection<BlockStatusMongoEntity>>();
            ioc.RegisterInstance(GetTransactionInputsCollection(settings)).As<IMongoCollection<TransactionInputMongoEntity>>();
            ioc.RegisterInstance(GetTransactionOutputsCollection(settings)).As<IMongoCollection<TransactionOutputMongoEntity>>();
            ioc.RegisterInstance(GetUnconfirmedBalanceChangesCollection(settings)).As<IMongoCollection<BalanceChangeMongoEntity>>();
            ioc.RegisterInstance(GetUnconfirmedBalanceStatusesCollection(settings)).As<IMongoCollection<TransactionStatusMongoEntity>>();
        }

        private static IMongoCollection<BlockStatusMongoEntity> GetBlockStatusesCollection(BaseSettings settings)
        {
            var client = new MongoClient(settings.NinjaData.ConnectionString);
            var db = client.GetDatabase(settings.NinjaData.DbName);

            return db.GetCollection<BlockStatusMongoEntity>(BlockStatusMongoEntity.CollectionName);
        }

        private static IMongoCollection<TransactionInputMongoEntity> GetTransactionInputsCollection(BaseSettings settings)
        {
            var client = new MongoClient(settings.NinjaData.ConnectionString);
            var db = client.GetDatabase(settings.NinjaData.DbName);

            return db.GetCollection<TransactionInputMongoEntity>(TransactionInputMongoEntity.CollectionName);
        }

        private static IMongoCollection<TransactionOutputMongoEntity> GetTransactionOutputsCollection(BaseSettings settings)
        {
            var client = new MongoClient(settings.NinjaData.ConnectionString);
            var db = client.GetDatabase(settings.NinjaData.DbName);

            return db.GetCollection<TransactionOutputMongoEntity>(TransactionOutputMongoEntity.CollectionName);
        }

        private static IMongoCollection<BalanceChangeMongoEntity> GetUnconfirmedBalanceChangesCollection(BaseSettings settings)
        {
            var client = new MongoClient(settings.UnconfirmedNinjaData.ConnectionString);
            var db = client.GetDatabase(settings.UnconfirmedNinjaData.DbName);

            return db.GetCollection<BalanceChangeMongoEntity>(BalanceChangeMongoEntity.CollectionName);
        }


        private static IMongoCollection<TransactionStatusMongoEntity> GetUnconfirmedBalanceStatusesCollection(BaseSettings settings)
        {
            var client = new MongoClient(settings.UnconfirmedNinjaData.ConnectionString);
            var db = client.GetDatabase(settings.UnconfirmedNinjaData.DbName);

            return db.GetCollection<TransactionStatusMongoEntity>(TransactionStatusMongoEntity.CollectionName);
        }
    }
}
