using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Core.Settings;
using Core.Transaction;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using NBitcoin;
using Repositories.Mongo;

namespace Repositories.Transactions
{

    public class SetSpendableOperationResult: ISetSpendableOperationResult
    {
        public IEnumerable<ITransactionInput> Ok { get; set; }

        public IEnumerable<ITransactionInput> NotFound { get; set; }

        public static SetSpendableOperationResult Create(IEnumerable<ITransactionInput> ok,
            IEnumerable<ITransactionInput> notFound)
        {
            return new SetSpendableOperationResult
            {
                NotFound = notFound,
                Ok = ok
            };
        }
    }


    public class TransactionOutputRepository: ITransactionOutputRepository
    {
        private readonly IMongoCollection<TransactionOutputMongoEntity> _collection;
        private readonly ILog _log;

        public TransactionOutputRepository(MongoSettings mongoSettings, ILog log)
        {
            _log = log;
            var client = new MongoClient(mongoSettings.ConnectionString);
            var db = client.GetDatabase(mongoSettings.DataDbName);
            _collection = db.GetCollection<TransactionOutputMongoEntity>(TransactionOutputMongoEntity.CollectionName);
        }

        public async Task InsertIfNotExists(IEnumerable<ITransactionOutput> items)
        {
            var allIds = items.Select(p => p.Id);
            var existed = await _collection.AsQueryable().Where(p => allIds.Contains(p.Id)).Select(p => p.Id).ToListAsync();

            //if (existed.Any())
            //{
            //    await _log.WriteWarningAsync(nameof(TransactionInputRepository), nameof(InsertIfNotExists),
            //        existed.Take(5).ToJson(),
            //        "Attempt To insert existed");
            //}

            var itemsToInsert = items.Where(p => !existed.Contains(p.Id)).ToList();

            if (itemsToInsert.Any())
            {
                await _collection.InsertManyAsync(itemsToInsert.Select(TransactionOutputMongoEntity.Create));
            }
        }

        public async Task<ISetSpendableOperationResult> SetSpended(IEnumerable<ITransactionInput> inputs)
        {
            var spendOutputIds = inputs.Select(
                input => TransactionOutputMongoEntity.GenerateId(input.TxIn.Id));


            var foundOutputs = await _collection.AsQueryable().Where(p => spendOutputIds.Contains(p.Id)).Select(p => p.Id).ToListAsync();

            var inputsDictionary = inputs.ToDictionary(
                p => TransactionOutputMongoEntity.GenerateId(p.TxIn.Id));

            if (foundOutputs.Any())
            {
                var bulkOps = new List<WriteModel<TransactionOutputMongoEntity>>();
                foreach (var id in foundOutputs)
                {
                    var input = inputsDictionary[id];

                    var updateOneOp = new UpdateOneModel<TransactionOutputMongoEntity>(
                        TransactionOutputMongoEntity.Filter.EqId(id), 
                        TransactionOutputMongoEntity.Update.SetSpended(input));

                    bulkOps.Add(updateOneOp);
                }

                await _collection.BulkWriteAsync(bulkOps);
            }


            var ok = foundOutputs.Select(id => inputsDictionary[id]).ToList();

            var notFoundInputs = spendOutputIds.Where(id => !foundOutputs.Contains(id)).Select(id => inputsDictionary[id]).ToList();

            return SetSpendableOperationResult.Create(ok, notFoundInputs);
        }

        public async Task<long> GetTransactionsCount(BitcoinAddress address, 
            int? at)
        {
            var stringAddress = address.ToWif();
            var query = _collection.AsQueryable()
                .Where(output => output.DestinationAddress == stringAddress);

            if (at != null)
            {
                query = query.Where(p => p.BlockHeight <= at);
            }

            return await query
                .Select(p => new[] {p.TransactionId, p.SpendTxInput.SpendedInTxId})
                .SelectMany(p => p)
                .Where(p => p!= null)
                .Distinct()
                .CountAsync();
        }

        public async Task<long> GetBtcAmountSummary(BitcoinAddress address, int? at = null, bool isColored = false)
        {
            var stringAddress = address.ToWif();
            var query = _collection.AsQueryable()
                .Where(output => output.DestinationAddress == stringAddress)
                .Where(p => !p.SpendTxInput.IsSpended);

            if (at != null)
            {
                query = query.Where(p => p.BlockHeight <= at);
            }

            if (isColored) // exclude btc with colored assets
            {
                query = query.Where(p => !p.ColoredData.HasColoredData);
            }

            return await query
                .SumAsync(p => p.BtcSatoshiAmount);
        }

        public async Task<long> GetBtcReceivedSummary(BitcoinAddress address, int? at, bool isColored = false)
        {
            var stringAddress = address.ToWif();
            var query = _collection.AsQueryable();
            query = query.Where(output => output.DestinationAddress == stringAddress);

            if (at != null)
            {
                query = query.Where(p => p.BlockHeight <= at);
            }

            if (isColored)
            {
                query = query.Where(p => !p.ColoredData.HasColoredData);
            }

            return await query
                .SumAsync(p => p.BtcSatoshiAmount);
        }

        public async Task<IDictionary<string, long>> GetAssetsReceived(BitcoinAddress address, int? at = null)
        {
            var stringAddress = address.ToWif();
            var query = _collection.AsQueryable()
                .Where(output => output.DestinationAddress == stringAddress)
                .Where(p => p.ColoredData.HasColoredData);

            if (at != null)
            {
                query = query.Where(p => p.BlockHeight <= at);
            }

            var result = await query
                .GroupBy(p => p.ColoredData.AssetId)
                .Select(p => new {assetId = p.Key, sum = p.Sum(x => x.ColoredData.Quantity)})
                .ToListAsync();

            return result.ToDictionary(p => p.assetId, p => p.sum);
        }

        public async Task<IDictionary<string, long>> GetAssetsAmount(BitcoinAddress address, int? at = null)
        {
            var stringAddress = address.ToWif();

            var query = _collection.AsQueryable()
                .Where(output => output.DestinationAddress == stringAddress)
                .Where(p => p.ColoredData.HasColoredData)
                .Where(p => !p.SpendTxInput.IsSpended);

            if (at != null)
            {
                query = query.Where(p => p.BlockHeight <= at);
            }

            var result = await query
                .GroupBy(p => p.ColoredData.AssetId)
                .Select(p => new { addr = p.Key, sum = p.Sum(x => x.ColoredData.Quantity) })
                .ToListAsync();

            return result.ToDictionary(p => p.addr, p => p.sum);
        }

        public async Task<IEnumerable<ITransactionOutput>> GetSpended(BitcoinAddress address, 
            int? minBlockHeight = null, 
            int? maxBlockHeight = null)
        {
            var stringAddress = address.ToWif();

            var query = _collection.AsQueryable()
                .Where(output => output.DestinationAddress == stringAddress)
                .Where(p => p.SpendTxInput.IsSpended)
                .Where(p => p.BtcSatoshiAmount != 0);

            if (minBlockHeight != null)
            {
                query = query.Where(p => p.SpendTxInput.BlockHeight >= minBlockHeight.Value);
            }

            if (maxBlockHeight != null)
            {
                query = query.Where(p => p.SpendTxInput.BlockHeight <= maxBlockHeight.Value);
            }

            return await query.ToListAsync();
        }

        public async Task<IEnumerable<ITransactionOutput>> GetReceived(BitcoinAddress address, 
            bool unspentOnly = false,
            int? minBlockHeight = null, 
            int? maxBlockHeight = null)
        {
            var stringAddress = address.ToWif();

            var query = _collection.AsQueryable()
                .Where(output => output.DestinationAddress == stringAddress)
                .Where(p => p.BtcSatoshiAmount != 0);

            if (minBlockHeight != null)
            {
                query = query.Where(p => p.BlockHeight >= minBlockHeight.Value);
            }

            if (maxBlockHeight != null)
            {
                query = query.Where(p => p.BlockHeight <= maxBlockHeight.Value);
            }


            if (unspentOnly)
            {
                query = query.Where(p => !p.SpendTxInput.IsSpended);
            }

            return await query.ToListAsync();
        }

        public async Task SetIndexes()
        {
            await InsertUniqueIndexes();
            var address = Builders<TransactionOutputMongoEntity>.IndexKeys.Ascending(p => p.DestinationAddress);

            var hasColoredData = Builders<TransactionOutputMongoEntity>.IndexKeys.Ascending(p => p.ColoredData.HasColoredData);

            var isSpended = Builders<TransactionOutputMongoEntity>.IndexKeys.Ascending(p => p.SpendTxInput.IsSpended);

            var transactionId = Builders<TransactionOutputMongoEntity>.IndexKeys.Ascending(p => p.TransactionId);
            var inputTransactionId = Builders<TransactionOutputMongoEntity>.IndexKeys.Ascending(p => p.SpendTxInput.SpendedInTxId);

            var assetId = Builders<TransactionOutputMongoEntity>.IndexKeys.Ascending(p => p.ColoredData.AssetId);

            var assetQuantity = Builders<TransactionOutputMongoEntity>.IndexKeys.Ascending(p => p.ColoredData.Quantity);
            var btcValue = Builders<TransactionOutputMongoEntity>.IndexKeys.Ascending(p => p.BtcSatoshiAmount);

            var blockHeight = Builders<TransactionOutputMongoEntity>.IndexKeys.Descending(p => p.BlockHeight);

            var inputBlockHeight = Builders<TransactionOutputMongoEntity>.IndexKeys.Descending(p => p.SpendTxInput.BlockHeight);

            await _collection.Indexes.CreateOneAsync(address);
            await _collection.Indexes.CreateOneAsync(isSpended);
            await _collection.Indexes.CreateOneAsync(assetId);
            await _collection.Indexes.CreateOneAsync(hasColoredData);
            await _collection.Indexes.CreateOneAsync(transactionId);
            await _collection.Indexes.CreateOneAsync(inputTransactionId);
            await _collection.Indexes.CreateOneAsync(blockHeight);
            await _collection.Indexes.CreateOneAsync(inputBlockHeight);

            var supportTxCount = Builders<TransactionOutputMongoEntity>.IndexKeys.Combine(address, transactionId);
            await _collection.Indexes.CreateOneAsync(supportTxCount,
                new CreateIndexOptions {Name = "supportTxCount", Background = true });


            //var supportTxCountByBlockIndex = Builders<TransactionOutputMongoEntity>.IndexKeys.Combine(address, transactionId, blockHeight);
            //await _collection.Indexes.CreateOneAsync(supportTxCountByBlockIndex,
            //    new CreateIndexOptions { Name = "supportTxCountByBlock", Background = true});

            //var supportBtcAmountSummaryIndex = Builders<TransactionOutputMongoEntity>.IndexKeys.Combine(address, hasColoredData,  btcValue);
            //await _collection.Indexes.CreateOneAsync(supportBtcAmountSummaryIndex,
            //    new CreateIndexOptions { Name = "supportBtcAmountSummaryIndex", Background = true });

            //var supportBtcAmountSummaryByBlockIndex = Builders<TransactionOutputMongoEntity>.IndexKeys.Combine(address, hasColoredData,  blockHeight, btcValue);
            //await _collection.Indexes.CreateOneAsync(supportBtcAmountSummaryByBlockIndex,
            //    new CreateIndexOptions { Name = "supportBtcAmountSummaryIndexByBlock", Background = true });

            //var supportBtcReceivedtSummaryIndex = Builders<TransactionOutputMongoEntity>.IndexKeys.Combine(address, hasColoredData, isSpended, btcValue);
            //await _collection.Indexes.CreateOneAsync(supportBtcReceivedtSummaryIndex,
            //    new CreateIndexOptions { Name = "supportBtcReceivedtSummaryIndex", Background = true });

            //var supportBtcReceivedSummaryByBlockIndex = Builders<TransactionOutputMongoEntity>.IndexKeys.Combine(address, hasColoredData, isSpended, blockHeight, btcValue);
            //await _collection.Indexes.CreateOneAsync(supportBtcReceivedSummaryByBlockIndex,
            //    new CreateIndexOptions { Name = "supportBtcReceivedSummaryByBlockIndex", Background = true });

        }

        public async Task InsertUniqueIndexes()
        {
            var idIndex = Builders<TransactionOutputMongoEntity>.IndexKeys.Descending(p => p.Id);

            await _collection.Indexes.CreateOneAsync(idIndex, new CreateIndexOptions { Unique = true });
        }
    }

    public class TransactionOutputMongoEntity: ITransactionOutput
    {
        public const string CollectionName = "transaction-outputs";

        [BsonId(IdGenerator = typeof(ObjectIdGenerator))]
        public ObjectId _id { get; set; }

        public string Id { get; set; }
        
        public string BlockId { get; set; }

        public int BlockHeight { get; set; }

        public string TransactionId { get; set; }

        public ulong Index { get; set; }

        public long BtcSatoshiAmount { get; set; }

        public string DestinationAddress { get; set; }

        IColoredOutputData ITransactionOutput.ColoredData => ColoredData.HasColoredData? ColoredData:null;

        public TransactionOutputColoredInfoMongoEntity ColoredData { get; set; }

        public static string GenerateId(string id)
        {
            return id;
        }
        
        public SpendTxInputMongoEntity SpendTxInput { get; set; }

        ISpendTxInput ITransactionOutput.SpendTxInput => SpendTxInput;

        public static TransactionOutputMongoEntity Create(ITransactionOutput source)
        {
            return new TransactionOutputMongoEntity
            {
                Id = GenerateId(source.Id),
                BlockHeight = source.BlockHeight,
                BlockId = source.BlockId,
                Index = source.Index,
                TransactionId = source.TransactionId,
                BtcSatoshiAmount = source.BtcSatoshiAmount,
                DestinationAddress = source.DestinationAddress,
                SpendTxInput = SpendTxInputMongoEntity.CreateNotSpended(),
                ColoredData = TransactionOutputColoredInfoMongoEntity.Create(source.ColoredData)
            };
        }

        public static class Filter
        {
            public static FilterDefinition<TransactionOutputMongoEntity> EqId(string id)
            {
                return Builders<TransactionOutputMongoEntity>.Filter.Eq(p => p.Id, id);
            }

            public static FilterDefinition<TransactionOutputMongoEntity> EqAddress(BitcoinAddress address)
            {
                return Builders<TransactionOutputMongoEntity>.Filter.Eq(p => p.DestinationAddress, address.ToWif());
            }
        }

        public static class Update
        {
            public static UpdateDefinition<TransactionOutputMongoEntity> SetSpended(ITransactionInput input)
            {
                return Builders<TransactionOutputMongoEntity>.Update.Set(p => p.SpendTxInput,
                    SpendTxInputMongoEntity.CreateSpended(input));
            }
        }
    }

    public class SpendTxInputMongoEntity: ISpendTxInput
    {
        public string Id { get; set; }

        public ulong Index { get; set; }

        public bool IsSpended { get; set; }

        public string SpendedInTxId { get; set; }

        public string BlockId { get; set; }

        public int BlockHeight { get; set; }

        public static SpendTxInputMongoEntity CreateSpended(ITransactionInput input)
        {
            return new SpendTxInputMongoEntity
            {
                IsSpended = true,
                SpendedInTxId = input.TransactionId,
                Id = input.Id,
                Index = input.Index,
                BlockHeight = input.BlockHeight,
                BlockId = input.BlockId
            };
        }

        public static SpendTxInputMongoEntity CreateNotSpended()
        {
            return new SpendTxInputMongoEntity
            {
                IsSpended = false,
                SpendedInTxId = null
            };
        }
    }

    public class TransactionOutputColoredInfoMongoEntity:IColoredOutputData
    {
        public string AssetId { get; set; }
        public long Quantity { get; set; }

        public bool HasColoredData { get; set; }

        public static TransactionOutputColoredInfoMongoEntity Create(IColoredOutputData source)
        {
            if (source != null)
            {
                return new TransactionOutputColoredInfoMongoEntity
                {
                    AssetId = source.AssetId,
                    Quantity = source.Quantity,
                    HasColoredData = true
                };
            }

            return new TransactionOutputColoredInfoMongoEntity
            {
                HasColoredData = false
            };
        }
    }
}
