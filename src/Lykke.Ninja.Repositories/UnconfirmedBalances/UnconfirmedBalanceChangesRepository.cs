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
using MongoDB.Driver.Linq;
using MoreLinq;

namespace Lykke.Ninja.Repositories.UnconfirmedBalances
{
    public class UnconfirmedBalanceChangesRepository: IUnconfirmedBalanceChangesRepository
    {
        private readonly IMongoCollection<BalanceChangeMongoEntity> _collection;

        private readonly Lazy<Task> _collectionPreparedLocker;
        private readonly IConsole _console;
        private readonly AggregateOptions _defaultAggregateOptions;

        public UnconfirmedBalanceChangesRepository(IConsole console, 
            IMongoCollection<BalanceChangeMongoEntity> collection)
        {
            _console = console;
            _collection = collection;
            _collectionPreparedLocker = new Lazy<Task>(PrepareCollection);

            _defaultAggregateOptions = new AggregateOptions { MaxTime = TimeSpan.FromSeconds(35) };
        }


        public async Task Upsert(IEnumerable<IBalanceChange> items)
        {
            await EnsureCollectionPrepared();

            WriteConsole($"{nameof(Upsert)} {items.Count()} items started");


            if (items.Any())
            {
                var updates = items.Select(p => new ReplaceOneModel<BalanceChangeMongoEntity>(
                        Builders<BalanceChangeMongoEntity>.Filter.Eq(x => x.Id, p.Id),
                        BalanceChangeMongoEntity.Create(p))
                    { IsUpsert = true });

                await _collection.BulkWriteAsync(updates, new BulkWriteOptions { IsOrdered = false });
            }

            WriteConsole($"{nameof(Upsert)} {items.Count()} items done");
        }

        public async Task Remove(IEnumerable<string> txIds)
        {
            await EnsureCollectionPrepared();
            if (txIds.Any())
            {
                await _collection.UpdateManyAsync(p => txIds.Contains(p.TxId), Builders<BalanceChangeMongoEntity>.Update.Set(p => p.Removed, true));
            }
        }
		

	    public async Task<long> GetTransactionsCount(string address)
        {
            await EnsureCollectionPrepared();

            var query = _collection.AsQueryable(_defaultAggregateOptions)
                .Where(p => p.Address == address)
                .Where(p=>!p.Removed);

            return await query.Select(p => p.TxId).Distinct().CountAsync();
        }

        public async Task<long> GetSpendTransactionsCount(string address)
        {
            await EnsureCollectionPrepared();

            var query = _collection.AsQueryable(_defaultAggregateOptions)
                .Where(p => !p.Removed)
                .Where(p => p.Address == address)
                .Where(p => p.IsInput);

            return await query.Select(p => p.TxId).Distinct().CountAsync();
        }

        public async Task<long> GetBtcAmountSummary(string address, bool isColored = false)
        {
            await EnsureCollectionPrepared();

            var query = _collection.AsQueryable(_defaultAggregateOptions)
                .Where(p => !p.Removed)
                .Where(p => p.Address == address);

            if (isColored)
            {
                query = query.Where(p => !p.HasColoredData);
            }

            return await query.SumAsync(p => p.BtcSatoshiAmount);
        }

        public async Task<long> GetBtcReceivedSummary(string address,  bool isColored = false)
        {
            await EnsureCollectionPrepared();

            var query = _collection.AsQueryable(_defaultAggregateOptions)
                .Where(p => !p.Removed)
                .Where(p => p.Address == address)
                .Where(p => !p.IsInput);

            if (isColored)
            {
                query = query.Where(p => !p.HasColoredData);
            }

            return await query.SumAsync(p => p.BtcSatoshiAmount);
        }

        public async Task<IReadOnlyDictionary<string, long>> GetAssetsReceived(string address)
        {
            await EnsureCollectionPrepared();

            var query = _collection.AsQueryable(_defaultAggregateOptions)
                .Where(p => !p.Removed)
                .Where(p => p.Address == address)
                .Where(p => !p.IsInput);

            var result = await query
                .GroupBy(p => p.AssetId)
                .Select(p => new { assetId = p.Key, sum = p.Sum(x => x.AssetQuantity) })
                .ToListAsync();


            return result.Where(p => !string.IsNullOrEmpty(p.assetId)).ToDictionary(p => p.assetId, p => p.sum);
        }

        public async Task<IReadOnlyDictionary<string, long>> GetAssetsAmount(string address)
        {
            await EnsureCollectionPrepared(); 

            var query = _collection.AsQueryable(_defaultAggregateOptions)
                .Where(p => !p.Removed)
                .Where(p => p.Address == address);

            var result = await query
                .GroupBy(p => p.AssetId)
                .Select(p => new { assetId = p.Key, sum = p.Sum(x => x.AssetQuantity) })
                .ToListAsync();


            return result.Where(p => !string.IsNullOrEmpty(p.assetId)).ToDictionary(p => p.assetId, p => p.sum);
        }

        public async Task<IEnumerable<IBalanceChange>> GetSpended(string address, bool isColored)
        {
            await EnsureCollectionPrepared();

            var query = _collection.AsQueryable(_defaultAggregateOptions)
                .Where(p => !p.Removed)
                .Where(p => p.Address == address)
                .Where(p => p.IsInput);

            if (isColored)
            {
                query = query.Where(p => !p.HasColoredData);
            }

            return await query.ToListAsync();
        }

        public async Task<IEnumerable<IBalanceChange>> GetReceived(string address, bool isColored)
        {
            await EnsureCollectionPrepared();

            var query = _collection.AsQueryable(_defaultAggregateOptions)
                .Where(p => !p.Removed)
                .Where(p => p.Address == address)
                .Where(p => !p.IsInput);

            if (isColored)
            {
                query = query.Where(p => !p.HasColoredData);
            }

            return await query.ToListAsync();
        }

        public async Task<IEnumerable<IBalanceChange>> GetByIds(IEnumerable<string> ids)
        {
            await EnsureCollectionPrepared();

            WriteConsole($"{nameof(GetByIds)} started. Ids {ids.Count()}");

            IEnumerable<IBalanceChange> result;

            if (ids.Any())
            {
                result =  await _collection.AsQueryable(_defaultAggregateOptions)
                    .Where(p => ids.Contains(p.Id))
                    .ToListAsync();
            }
            else
            {
                result = Enumerable.Empty<IBalanceChange>();
            }


            WriteConsole($"{nameof(GetByIds)} started. Ids  {result.Count()} of {ids.Count()}");

            return result;
        }

	    public async Task UpdateExpiration(IEnumerable<string> txIds)
		{
			await EnsureCollectionPrepared();
			if (txIds.Any())
			{
				await _collection.UpdateManyAsync(p => txIds.Contains(p.TxId), Builders<BalanceChangeMongoEntity>.Update.Set(p => p.Changed, DateTime.UtcNow));
			}
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
                SetTxIdIndex(),
                SetCommonIndex(),
                SetExpirationIndex()
            };

            await Task.WhenAll(setIndexes);

            WriteConsole($"{nameof(PrepareCollection)} done");
        }

        private async Task SetTxIdIndex()
        {
            var ind = Builders<BalanceChangeMongoEntity>.IndexKeys.Descending(p => p.TxId);
            await _collection.Indexes.CreateOneAsync(ind);
        }

        private async Task SetCommonIndex()
        {
            var removed = Builders<BalanceChangeMongoEntity>.IndexKeys.Ascending(p => p.Removed);
            var addr = Builders<BalanceChangeMongoEntity>.IndexKeys.Descending(p => p.Address);
            var isInput = Builders<BalanceChangeMongoEntity>.IndexKeys.Ascending(p => p.IsInput);
            var hasColoredData = Builders<BalanceChangeMongoEntity>.IndexKeys.Ascending(p => p.HasColoredData);


            var combine = Builders<BalanceChangeMongoEntity>.IndexKeys.Combine(removed, addr, isInput, hasColoredData);
            await _collection.Indexes.CreateOneAsync(combine, 
                new CreateIndexOptions<BalanceChangeMongoEntity>
                {
                    PartialFilterExpression = Builders<BalanceChangeMongoEntity>.Filter.Eq(p => p.Removed, false)
                });
        }

        private async Task SetExpirationIndex()
        {
            var changed = Builders<BalanceChangeMongoEntity>.IndexKeys.Descending(p => p.Changed);
            
            await _collection.Indexes.CreateOneAsync(changed, new CreateIndexOptions{ ExpireAfter = TimeSpan.FromHours(6)});
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

        [BsonElement("sptxid")]
        public string SpendTxId { get; set; }

        [BsonElement("sptxind")]
        public ulong? SpendTxInput { get; set; }

        [BsonElement("chng")]
        public DateTime Changed { get; set; }

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
                HasColoredData = source.HasColoredData,
                AssetQuantity = source.AssetQuantity,
                SpendTxInput = source.SpendTxInput,
                SpendTxId = source.SpendTxId,
                Changed = DateTime.UtcNow
            };
        }
    }
}
