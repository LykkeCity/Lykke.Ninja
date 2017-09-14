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
using NBitcoin;

namespace Lykke.Ninja.Repositories.UnconfirmedBalances
{
    public class UnconfirmedTransactionStatusesRepository: IUnconfirmedTransactionStatusesRepository
    {
        private readonly IMongoCollection<UnconfirmedTransactionStatusMongoEntity> _collection;
        private readonly IMongoDatabase _db;
        private readonly BaseSettings _baseSettings;

        private readonly Lazy<Task> _collectionPreparedLocker;
        private readonly IConsole _console;

        public UnconfirmedTransactionStatusesRepository(BaseSettings settings, IConsole console)
        {
            _baseSettings = settings;
            _console = console;
            _collectionPreparedLocker = new Lazy<Task>(PrepareCollection);

            var client = new MongoClient(settings.UnconfirmedNinjaData.ConnectionString);
            _db = client.GetDatabase(settings.UnconfirmedNinjaData.DbName);

            _collection =
                _db.GetCollection<UnconfirmedTransactionStatusMongoEntity>(UnconfirmedTransactionStatusMongoEntity
                    .CollectionName);
        }


        public  async Task Insert(IEnumerable<IUnconfirmedTransactionStatus> items)
        {
            await EnsureCollectionPrepared();


            await _collection.InsertManyAsync(items.Select(UnconfirmedTransactionStatusMongoEntity.Create), new InsertManyOptions { IsOrdered = false });
        }

        public async Task SetProcessingStatus(IEnumerable<string> txIds, UnconfirmedTransactionProcessingStatus status)
        {
            await EnsureCollectionPrepared();
            
            var updatedStatusValue = (int) status;

            await _collection.UpdateManyAsync(p => txIds.Contains(p.TxId), Builders<UnconfirmedTransactionStatusMongoEntity>.Update.Set(p => p.Status, updatedStatusValue));
        }

        public async Task Confirm(IEnumerable<string> txIds)
        {
            await EnsureCollectionPrepared();
            await _collection.UpdateManyAsync(p => txIds.Contains(p.TxId), Builders<UnconfirmedTransactionStatusMongoEntity>.Update.Set(p => p.Confirmed, true));
        }

        public async Task<IEnumerable<string>> GetUnconfirmedIds()
        {
            await EnsureCollectionPrepared();
            return await _collection.AsQueryable().Where(p => !p.Confirmed).Select(p => p.TxId).ToListAsync();
        }

        private Task EnsureCollectionPrepared()
        {
            return _collectionPreparedLocker.Value;
        }

        private async Task PrepareCollection()
        {
            _console.WriteLine($"{nameof(PrepareCollection)} started");

            if (!await _db.IsCollectionExists(UnconfirmedTransactionStatusMongoEntity.CollectionName))
            {
                await _db.CreateCollectionAsync(
                    UnconfirmedTransactionStatusMongoEntity.CollectionName,
                    new CreateCollectionOptions
                    {
                        Capped = true,
                        MaxDocuments = _baseSettings.UnconfirmedNinjaData.TransactionStatusesCappedCollectionMaxDocuments,
                        MaxSize = _baseSettings.UnconfirmedNinjaData.TransactionStatusesCappedCollectionMaxSize

                    });
            }


            var setIndexes = new[]
            {
                SetConfirmedIndex(),
            };

            await Task.WhenAll(setIndexes);

            _console.WriteLine($"{nameof(PrepareCollection)} done");
        }

        private async Task SetConfirmedIndex()
        {
            var ind = Builders<UnconfirmedTransactionStatusMongoEntity>.IndexKeys.Descending(p => p.Confirmed);
            await _collection.Indexes.CreateOneAsync(ind);
        }
    }

    public class UnconfirmedTransactionStatusMongoEntity: IUnconfirmedTransactionStatus
    {
        public const string CollectionName = "unconfirmed-transaction-statuses";

        [BsonId]
        [BsonElement("txid")]
        public string TxId { get; set; }

        [BsonElement("created")]
        public DateTime Created { get; set; }

        [BsonElement("changed")]
        public DateTime LastStatusChange { get; set; }

        [BsonElement("confirmed")]
        public bool Confirmed { get; set; }

        [BsonElement("status")]
        public int Status { get; set; }

        UnconfirmedTransactionProcessingStatus IUnconfirmedTransactionStatus.Status => (UnconfirmedTransactionProcessingStatus) Status;

        public static UnconfirmedTransactionStatusMongoEntity Create(IUnconfirmedTransactionStatus source)
        {
            return new UnconfirmedTransactionStatusMongoEntity
            {
                Confirmed = source.Confirmed,
                Created = source.Created,
                LastStatusChange = source.LastStatusChange,
                Status = (int) source.Status,
                TxId = source.TxId
            };
        }
    }
}
