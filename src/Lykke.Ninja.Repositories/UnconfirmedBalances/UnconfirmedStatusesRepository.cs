using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Ninja.Core.Settings;
using Lykke.Ninja.Core.UnconfirmedBalances.Statuses;
using Lykke.Ninja.Repositories.Mongo;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Lykke.Ninja.Repositories.UnconfirmedBalances
{
    public class UnconfirmedStatusesRepository: IUnconfirmedStatusesRepository
    {
        private readonly IMongoCollection<TransactionStatusMongoEntity> _collection;

        private readonly Lazy<Task> _collectionPreparedLocker;
        private readonly IConsole _console;

        public UnconfirmedStatusesRepository(BaseSettings settings, IConsole console)
        {
            _console = console;
            _collectionPreparedLocker = new Lazy<Task>(PrepareCollection);

            var client = new MongoClient(settings.UnconfirmedNinjaData.ConnectionString);
            var db = client.GetDatabase(settings.UnconfirmedNinjaData.DbName);

            _collection =
                db.GetCollection<TransactionStatusMongoEntity>(TransactionStatusMongoEntity
                    .CollectionName);
        }


        public  async Task Upsert(IEnumerable<ITransactionStatus> items)
        {
            await EnsureCollectionPrepared();

            if (items.Any())
            {
                var updates = items.Select(p => new ReplaceOneModel<TransactionStatusMongoEntity>(
                        Builders<TransactionStatusMongoEntity>.Filter.Eq(x => x.TxId, p.TxId),
                        TransactionStatusMongoEntity.Create(p))
                    { IsUpsert = true });

                await _collection.BulkWriteAsync(updates, new BulkWriteOptions { IsOrdered = false });
            }
        }

        public async Task SetInsertStatus(IEnumerable<string> txIds, InsertProcessStatus status)
        {
            await EnsureCollectionPrepared();

            WriteConsole($"{nameof(SetInsertStatus)} {status.ToString()} for {txIds.Count()} started");

            if (txIds.Any())
            {
                var updatedStatusValue = (int)status;
                await _collection.UpdateManyAsync(p => txIds.Contains(p.TxId),
                    Builders<TransactionStatusMongoEntity>.Update.Set(p => p.InsertProcessStatus, updatedStatusValue).Set(p=>p.Changed, DateTime.Now));
            }

            WriteConsole($"{nameof(SetInsertStatus)} {status.ToString()} done");
        }

        public async Task SetRemovedProcessingStatus(IEnumerable<string> txIds, RemoveProcessStatus status)
        {
            await EnsureCollectionPrepared();

            if (txIds.Any())
            {
                var updatedStatusValue = (int)status;
                await _collection.UpdateManyAsync(p => txIds.Contains(p.TxId),
                    Builders<TransactionStatusMongoEntity>.Update.Set(p => p.RemoveProcessStatus, updatedStatusValue).Set(p => p.Changed, DateTime.Now));
            }
        }

        public async Task Remove(IEnumerable<string> txIds, RemoveProcessStatus status)
        {
            await EnsureCollectionPrepared();

            if (txIds.Any())
            {
                var numStatus = (int) status;
                await _collection.UpdateManyAsync(p => txIds.Contains(p.TxId), Builders<TransactionStatusMongoEntity>.Update.Set(p => p.Removed, true).Set(p => p.RemoveProcessStatus, numStatus));
            }
        }

        public async Task<IEnumerable<string>> GetAllTxIds()
        {
            await EnsureCollectionPrepared();

            return await _collection.AsQueryable().Where(p => !p.Removed).Select(p => p.TxId).ToListAsync();
        }

        public async Task<long> GetAllTxCount()
        {
            await EnsureCollectionPrepared();
            return await _collection.AsQueryable().Where(p => !p.Removed).CountAsync();
        }

        public async Task<IEnumerable<string>> GetNotRemovedTxIds(InsertProcessStatus[] statuses)
        {
            var numStatuses = statuses.Select(p=>(int)p);
            return await _collection.AsQueryable()
                .Where(p => numStatuses.Contains(p.InsertProcessStatus))
                .Where(p => !p.Removed)
                .Select(p => p.TxId)
                .ToListAsync();
        }

        public async Task<long> GetNotRemovedTxCount(params InsertProcessStatus[] statuses)
        {
            var numStatuses = statuses.Select(p => (int)p);

            return await _collection.AsQueryable()
                .Where(p => numStatuses.Contains(p.InsertProcessStatus))
                .Where(p => !p.Removed)
                .CountAsync();}

        public async Task<IEnumerable<string>> GetRemovedTxIds(RemoveProcessStatus[] statuses)
        {
            var numStatuses = statuses.Select(p => (int)p);
            return await _collection.AsQueryable()
                .Where(p => numStatuses.Contains(p.RemoveProcessStatus))
                .Where(p => p.Removed)
                .Select(p => p.TxId).ToListAsync();
        }

        private Task EnsureCollectionPrepared()
        {
            return _collectionPreparedLocker.Value;
        }

        private async Task PrepareCollection()
        {
            WriteConsole($"{nameof(PrepareCollection)} started");

            var setIndexes = new[]
            {
                SetConfirmedIndex(),
                SetInsertProcessStatusQueryIndexIndex(),
                SetRemoveProcessStatusQueryIndex(),
                SetExpirationIndex()
            };

            await Task.WhenAll(setIndexes);

            WriteConsole($"{nameof(PrepareCollection)} done");
        }

        private void WriteConsole(string message)
        {
            _console.WriteLine($"{nameof(UnconfirmedStatusesRepository)}{message}");
        }

        private async Task SetConfirmedIndex()
        {
            var ind = Builders<TransactionStatusMongoEntity>.IndexKeys.Descending(p => p.Removed);
            await _collection.Indexes.CreateOneAsync(ind);
        }

        private async Task SetInsertProcessStatusQueryIndexIndex()
        {
            var statusIndex = Builders<TransactionStatusMongoEntity>.IndexKeys.Descending(p => p.InsertProcessStatus);
            var isRemovedIndex = Builders<TransactionStatusMongoEntity>.IndexKeys.Ascending(p => p.Removed);

            var combine = Builders<TransactionStatusMongoEntity>.IndexKeys.Combine(statusIndex, isRemovedIndex);

            await _collection.Indexes.CreateOneAsync(combine);
        }
        private async Task SetRemoveProcessStatusQueryIndex()
        {
            var statusIndex = Builders<TransactionStatusMongoEntity>.IndexKeys.Descending(p => p.RemoveProcessStatus);
            var isRemovedIndex = Builders<TransactionStatusMongoEntity>.IndexKeys.Descending(p => p.Removed);

            var combine = Builders<TransactionStatusMongoEntity>.IndexKeys.Combine(statusIndex, isRemovedIndex);

            await _collection.Indexes.CreateOneAsync(combine);
        }

        private async Task SetExpirationIndex()
        {
            var changed = Builders<TransactionStatusMongoEntity>.IndexKeys.Descending(p => p.Changed);

            await _collection.Indexes.CreateOneAsync(changed, new CreateIndexOptions { ExpireAfter = TimeSpan.FromHours(3) });
        }
    }

    public class TransactionStatusMongoEntity: ITransactionStatus
    {
        public const string CollectionName = "unconfirmed-statuses";

        [BsonId]
        [BsonElement("txid")]
        public string TxId { get; set; }

        [BsonElement("cr")]
        public DateTime Created { get; set; }

        [BsonElement("chng")]
        public DateTime Changed { get; set; }

        [BsonElement("rem")]
        public bool Removed { get; set; }

        [BsonElement("inst-st")]
        public int InsertProcessStatus { get; set; }

        [BsonElement("rem-st")]
        public int RemoveProcessStatus { get; set; }

        InsertProcessStatus ITransactionStatus.InsertProcessStatus => (InsertProcessStatus)InsertProcessStatus;
        RemoveProcessStatus ITransactionStatus.RemoveProcessStatus => (RemoveProcessStatus)RemoveProcessStatus;

        public static TransactionStatusMongoEntity Create(ITransactionStatus source)
        {
            return new TransactionStatusMongoEntity
            {
                Removed = source.Removed,
                Created = source.Created,
                Changed = source.Changed,
                InsertProcessStatus = (int) source.InsertProcessStatus,
                RemoveProcessStatus = (int) source.RemoveProcessStatus,
                TxId = source.TxId
            };
        }
    }
}
