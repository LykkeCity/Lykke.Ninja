using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Settings;
using Core.Transaction;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

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

        public TransactionOutputRepository(BaseSettings baseSettings)
        {
            var client = new MongoClient(baseSettings.NinjaData.ConnectionString);
            var db = client.GetDatabase(baseSettings.NinjaData.DbName);
            _collection = db.GetCollection<TransactionOutputMongoEntity>(TransactionOutputMongoEntity.CollectionName);
        }

        public async Task Insert(IEnumerable<ITransactionOutput> outputs)
        {
            if (outputs.Any())
            {
                await _collection.InsertManyAsync(outputs.Select(TransactionOutputMongoEntity.Create));
            }
        }

        public Task SetSpended(ITransactionInput input)
        {
            var id = TransactionOutputMongoEntity.GenerateId(input.InputTxIn.TransactionId, input.InputTxIn.Index);

            return _collection.UpdateOneAsync(
                TransactionOutputMongoEntity.Filter.EqId(id), TransactionOutputMongoEntity.Update.SetSpended(input.TransactionId));
        }

        public async Task<ISetSpendableOperationResult> SetSpended(IEnumerable<ITransactionInput> inputs)
        {
            var spendOutputIds = inputs.Select(
                input => TransactionOutputMongoEntity.GenerateId(input.InputTxIn.TransactionId, input.InputTxIn.Index));


            var foundOutputs = await _collection.AsQueryable().Where(p => spendOutputIds.Contains(p.Id)).Select(p => p.Id).ToListAsync();

            var inputsDictionary = inputs.ToDictionary(
                p => TransactionOutputMongoEntity.GenerateId(p.InputTxIn.TransactionId, p.InputTxIn.Index));

            if (foundOutputs.Any())
            {
                var bulkOps = new List<WriteModel<TransactionOutputMongoEntity>>();
                foreach (var id in foundOutputs)
                {
                    var input = inputsDictionary[id];

                    var updateOneOp = new UpdateOneModel<TransactionOutputMongoEntity>(
                        TransactionOutputMongoEntity.Filter.EqId(id), 
                        TransactionOutputMongoEntity.Update.SetSpended(input.TransactionId));

                    bulkOps.Add(updateOneOp);
                }

                await _collection.BulkWriteAsync(bulkOps);
            }


            var ok = foundOutputs.Select(id => inputsDictionary[id]).ToList();

            var notFoundInputs = spendOutputIds.Where(id => !foundOutputs.Contains(id)).Select(id => inputsDictionary[id]).ToList();

            return SetSpendableOperationResult.Create(ok, notFoundInputs);
        }
    }

    public class TransactionOutputMongoEntity
    {
        public const string CollectionName = "transaction-outputs";

        [BsonId]
        public string Id { get; set; }
        
        public string BlockId { get; set; }

        public int BlockHeight { get; set; }

        public string TransactionId { get; set; }

        public uint Index { get; set; }

        public long BtcSatoshiAmount { get; set; }

        public string DestinationAddress { get; set; }

        public static string GenerateId(string transactionId, uint index)
        {
            return $"{transactionId}_{index}";
        }
        
        public TransactionOutputSpendInfoMongoEntity SpendInfo { get; set; }

        public static TransactionOutputMongoEntity Create(ITransactionOutput source)
        {
            return new TransactionOutputMongoEntity
            {
                Id = GenerateId(source.TransactionId, source.Index),
                BlockHeight = source.BlockHeight,
                BlockId = source.BlockId,
                Index = source.Index,
                TransactionId = source.TransactionId,
                BtcSatoshiAmount = source.BtcSatoshiAmount,
                DestinationAddress = source.DestinationAddress,
                SpendInfo = TransactionOutputSpendInfoMongoEntity.CreateNotSpended()
            };
        }

        public static class Filter
        {
            public static FilterDefinition<TransactionOutputMongoEntity> EqId(string id)
            {
                return Builders<TransactionOutputMongoEntity>.Filter.Eq(p => p.Id, id);
            }
        }

        public static class Update
        {
            public static UpdateDefinition<TransactionOutputMongoEntity> SetSpended(string transactionId)
            {
                return Builders<TransactionOutputMongoEntity>.Update.Set(p => p.SpendInfo,
                    TransactionOutputSpendInfoMongoEntity.CreateSpended(transactionId));
            }
        }
    }

    public class TransactionOutputSpendInfoMongoEntity
    {
        public bool IsSpended { get; set; }

        public string SpendedInTxId { get; set; }

        public static TransactionOutputSpendInfoMongoEntity CreateSpended(string transactionId)
        {
            return new TransactionOutputSpendInfoMongoEntity
            {
                IsSpended = true,
                SpendedInTxId = transactionId
            };
        }

        public static TransactionOutputSpendInfoMongoEntity CreateNotSpended()
        {
            return new TransactionOutputSpendInfoMongoEntity
            {
                IsSpended = false,
                SpendedInTxId = null
            };
        }
    }
}
