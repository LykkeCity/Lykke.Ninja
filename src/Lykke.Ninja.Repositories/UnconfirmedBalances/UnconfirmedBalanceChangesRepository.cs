using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Ninja.Core.Settings;
using Lykke.Ninja.Core.UnconfirmedBalances.BalanceChanges;
using Lykke.Ninja.Repositories.Mongo;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace Lykke.Ninja.Repositories.UnconfirmedBalances
{
    public class UnconfirmedBalanceChangesRepository: IUnconfirmedBalanceChangesRepository
    {
        private readonly IMongoCollection<BalanceChangeMongoEntity> _collection;
        private readonly IMongoDatabase _db;
        private readonly BaseSettings _baseSettings;

        private readonly Lazy<Task> _collectionPreparedLocker;
        private readonly IConsole _console;

        public UnconfirmedBalanceChangesRepository(BaseSettings settings, IConsole console)
        {
            _baseSettings = settings;
            _console = console;
            _collectionPreparedLocker = new Lazy<Task>(PrepareCollection);

            var client = new MongoClient(settings.UnconfirmedNinjaData.ConnectionString);
            _db = client.GetDatabase(settings.UnconfirmedNinjaData.DbName);

            _collection =
                _db.GetCollection<BalanceChangeMongoEntity>(BalanceChangeMongoEntity
                    .CollectionName);
        }


        public async Task Upsert(IEnumerable<IBalanceChange> items)
        {
            await EnsureCollectionPrepared();

            if (items.Any())
            {
                var updates = items.Select(p => new ReplaceOneModel<BalanceChangeMongoEntity>(
                        Builders<BalanceChangeMongoEntity>.Filter.Eq(x => x.Id, p.Id),
                        BalanceChangeMongoEntity.Create(p))
                    { IsUpsert = true });

                await _collection.BulkWriteAsync(updates, new BulkWriteOptions { IsOrdered = false });
            }
        }

        public async Task Remove(IEnumerable<string> txIds)
        {
            await EnsureCollectionPrepared();
            if (txIds.Any())
            {
                await _collection.UpdateManyAsync(p => txIds.Contains(p.TxId), Builders<BalanceChangeMongoEntity>.Update.Set(p => p.Removed, true));
            }
        }

        public async Task<long> GetTransactionsCount(string address, int? at = null)
        {
            await EnsureCollectionPrepared();
            throw new NotImplementedException();
        }

        public async Task<long> GetSpendTransactionsCount(string address, int? at = null)
        {
            await EnsureCollectionPrepared();
            throw new NotImplementedException();
        }

        public async Task<long> GetBtcAmountSummary(string address, int? at = null, bool isColored = false)
        {
            await EnsureCollectionPrepared();
            throw new NotImplementedException();
        }

        public async Task<long> GetBtcReceivedSummary(string address, int? at = null, bool isColored = false)
        {
            await EnsureCollectionPrepared();
            throw new NotImplementedException();
        }

        public async Task<IDictionary<string, long>> GetAssetsReceived(string address, int? at = null)
        {
            await EnsureCollectionPrepared();
            throw new NotImplementedException();
        }

        public async Task<IDictionary<string, long>> GetAssetsAmount(string address, int? at = null)
        {
            await EnsureCollectionPrepared();
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<IBalanceChange>> GetSpended(string address, int? minBlockHeight = null, int? maxBlockHeight = null, int? itemsToSkip = null,
            int? itemsToTake = null)
        {
            await EnsureCollectionPrepared();
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<IBalanceChange>> GetReceived(string address, bool unspendOnly, int? minBlockHeight = null, int? maxBlockHeight = null,
            int? itemsToSkip = null, int? itemsToTake = null)
        {
            await EnsureCollectionPrepared();
            throw new NotImplementedException();
        }

        private Task EnsureCollectionPrepared()
        {
            return _collectionPreparedLocker.Value;
        }

        private async Task PrepareCollection()
        {
            WriteConsole($"{nameof(PrepareCollection)} started");

            if (!await _db.IsCollectionExistsAsync(BalanceChangeMongoEntity.CollectionName))
            {
                await _db.CreateCollectionAsync(
                    BalanceChangeMongoEntity.CollectionName,
                    new CreateCollectionOptions
                    {
                        Capped = true,
                        MaxDocuments = _baseSettings.UnconfirmedNinjaData.StatusesCappedCollectionMaxDocuments,
                        MaxSize = _baseSettings.UnconfirmedNinjaData.ChangesCappedCollectionMaxSize

                    });
            }
            
            var setIndexes = new[]
            {
                SetTxIdIndex(),
            };

            await Task.WhenAll(setIndexes);

            WriteConsole($"{nameof(PrepareCollection)} done");
        }

        private async Task SetTxIdIndex()
        {
            var ind = Builders<BalanceChangeMongoEntity>.IndexKeys.Descending(p => p.TxId);
            await _collection.Indexes.CreateOneAsync(ind);
        }

        private void WriteConsole(string message)
        {
            _console.WriteLine($"{nameof(UnconfirmedBalanceChangesRepository)}{message}");
        }

    }

    public class BalanceChangeMongoEntity: IBalanceChange
    {
        public const string CollectionName = "unconfirmed-changes";

        [BsonId]
        public string Id { get; set; }

        [BsonElement("txid")]
        public string TxId { get; set; }
        [BsonElement("ind")]
        public ulong Index { get; set; }

        [BsonElement("inp")]
        public bool IsInput { get; set; }

        [BsonElement("value")]
        public long BtcSatoshiAmount { get; set; }

        [BsonElement("addr")]
        public string Address { get; set; }

        [BsonElement("rem")]
        public bool Removed { get; set; }
        [BsonElement("ass")]
        public string AssetId { get; set; }

        [BsonElement("qty")]
        public long AssetQuantity { get; set; }

        [BsonElement("hcd")]
        public bool HasColoredData { get; set; }


        public static BalanceChangeMongoEntity Create(IBalanceChange source)
        {
            return new BalanceChangeMongoEntity
            {
                TxId = source.TxId,
                Address = source.Address,
                BtcSatoshiAmount = source.BtcSatoshiAmount,
                Index = source.Index,
                Removed = false,
                IsInput = source.IsInput,
                Id = source.Id,
                AssetId = source.AssetId,
                HasColoredData = source.AssetId != null,
                AssetQuantity = source.AssetQuantity
            };
        }
    }
}
