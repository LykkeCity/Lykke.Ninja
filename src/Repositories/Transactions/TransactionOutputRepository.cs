using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Core.Settings;
using Core.Transaction;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using NBitcoin;

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

        public TransactionOutputRepository(BaseSettings baseSettings, ILog log)
        {
            _log = log;
            var client = new MongoClient(baseSettings.NinjaData.ConnectionString);
            var db = client.GetDatabase(baseSettings.NinjaData.DbName);
            _collection = db.GetCollection<TransactionOutputMongoEntity>(TransactionOutputMongoEntity.CollectionName);
        }

        public async Task InsertIfNotExists(IEnumerable<ITransactionOutput> items)
        {
            var allIds = items.Select(p => p.Id);
            var existed = await _collection.AsQueryable().Where(p => allIds.Contains(p.Id)).Select(p => p.Id).ToListAsync();

            if (existed.Any())
            {
                await _log.WriteWarningAsync(nameof(TransactionInputRepository), nameof(InsertIfNotExists),
                    existed.Take(5).ToJson(),
                    "Attempt To insert existed");
            }

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

        public async Task<long> GetTransactionsCount(BitcoinAddress address, int? at)
        {

            return await TransactionOutputMongoEntity.Filter.Expressions
                .FilterBalances(_collection.AsQueryable(), address, at, unSpendOnly: false)
                .Select(p => new[] {p.TransactionId, p.SpendTxInput.SpendedInTxId})
                .SelectMany(p=>p)
                .Where(p => p!=null)
                .Distinct()
                .CountAsync();
        }

        public async Task<long> GetBtcAmountSummary(BitcoinAddress address, int? at = null, bool isColored = false)
        {
            var query = TransactionOutputMongoEntity.Filter.Expressions.FilterBalances(_collection.AsQueryable(),
                address, at, unSpendOnly: true);

            if (isColored)
            {
                query = query.Where(p => p.ColoredData == null);
            }

            return await query
                .SumAsync(p => p.BtcSatoshiAmount);
        }

        public async Task<long> GetBtcReceivedSummary(BitcoinAddress address, int? at, bool isColored = false)
        {
            var query = TransactionOutputMongoEntity.Filter.Expressions.FilterBalances(_collection.AsQueryable(),
                address, at,
                unSpendOnly: false);

            if (isColored)
            {
                query = query.Where(p => p.ColoredData == null);
            }
            return await query
                .SumAsync(p => p.BtcSatoshiAmount);
        }

        public async Task<IDictionary<string, long>> GetAssetsReceived(BitcoinAddress address, int? at = null)
        {
            var query = TransactionOutputMongoEntity.Filter.Expressions.FilterBalances(_collection.AsQueryable(),
                    address,
                    at,
                    unSpendOnly: false);

            var result = await query
                .GroupBy(p => p.ColoredData.AssetId)
                .Select(p => new {addr = p.Key, sum = p.Sum(x => x.ColoredData.Quantity)})
                .ToListAsync();

            return result.ToDictionary(p => p.addr, p => p.sum);
        }

        public async Task<IDictionary<string, long>> GetAssetsAmount(BitcoinAddress address, int? at = null)
        {
            var query = TransactionOutputMongoEntity.Filter.Expressions.FilterBalances(_collection.AsQueryable(),
                    address,
                    at,
                    unSpendOnly: true)
                .Where(p => p.ColoredData != null);

            var result = await query
                .GroupBy(p => p.ColoredData.AssetId)
                .Select(p => new { addr = p.Key, sum = p.Sum(x => x.ColoredData.Quantity) })
                .ToListAsync();

            return result.ToDictionary(p => p.addr, p => p.sum);
        }

        public async Task<IEnumerable<ITransactionOutput>> GetSpended(BitcoinAddress address, int? at = null)
        {
            var query = TransactionOutputMongoEntity.Filter.Expressions.FilterBalances(_collection.AsQueryable(),
                address,
                at)
                .Where(p => p.SpendTxInput.IsSpended)
                .Where(p => p.BtcSatoshiAmount != 0);


            return await query.ToListAsync();
            //var query = TransactionOutputMongoEntity.Filter.Expressions.FilterBalances(_collection.AsQueryable(),
            //    address,
            //    at)
            //    .Where(p=>p.SpendTxInput.IsSpended);

            //var lookupQuery = from left in query
            //    join right in _collection.AsQueryable() on left.SpendTxInput.Id equals right.Id into joined
            //    select joined;

            //var t = (await query.ToListAsync()).Where(p => p.SpendTxInput.IsSpended);
            //var t2 = await lookupQuery.ToListAsync();

            //var tstr = t.Select(p => p.SpendTxInput.Id);
            //var resstr = t2.SelectMany(p => p).Select(p => p.Id);
            //return (await lookupQuery.ToListAsync()).SelectMany(p=>p).Where(p => p.BtcSatoshiAmount > 0);
        }

        public async Task<IEnumerable<ITransactionOutput>> GetReceived(BitcoinAddress address, int? at = null)
        {
            var query = TransactionOutputMongoEntity.Filter.Expressions.FilterBalances(_collection.AsQueryable(),
                address,
                at).Where(p => p.BtcSatoshiAmount != 0);

            return await query.ToListAsync();
        }
    }

    public class TransactionOutputMongoEntity: ITransactionOutput
    {
        public const string CollectionName = "transaction-outputs";

        [BsonId]
        public string Id { get; set; }
        
        public string BlockId { get; set; }

        public int BlockHeight { get; set; }

        public string TransactionId { get; set; }

        public ulong Index { get; set; }

        public long BtcSatoshiAmount { get; set; }

        public string DestinationAddress { get; set; }
        IColoredOutputData ITransactionOutput.ColoredData => ColoredData;

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
            
            public static class Expressions
            {
                public static IMongoQueryable<TransactionOutputMongoEntity> FilterBalances(IMongoQueryable<TransactionOutputMongoEntity>  source, 
                    BitcoinAddress address, int? at, 
                    bool unSpendOnly = false)
                {
                    var stringAddress = address.ToWif();

                    var result = source.Where(output => output.DestinationAddress == stringAddress);
                        
                    if (at != null)
                    {

                        result = result.Where(p => p.BlockHeight <= at);

                    }

                    if (unSpendOnly)
                    {

                        result = result.Where(p => !p.SpendTxInput.IsSpended);

                    }

                    return result;
                }
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

        public static TransactionOutputColoredInfoMongoEntity Create(IColoredOutputData source)
        {
            if (source != null)
            {
                return new TransactionOutputColoredInfoMongoEntity
                {
                    AssetId = source.AssetId,
                    Quantity = source.Quantity
                };
            }

            return null;
        }
    }
}
