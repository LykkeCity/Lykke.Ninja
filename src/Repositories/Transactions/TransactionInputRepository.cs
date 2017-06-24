using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Core.Settings;
using Core.Transaction;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Repositories.Transactions
{
    public class TransactionInputRepository: ITransactionInputRepository
    {
        private readonly IMongoCollection<TransactionInputMongoEntity> _collection;
        private readonly ILog _log;

        public TransactionInputRepository(BaseSettings baseSettings, 
            ILog log)
        {
            _log = log;
            var client = new MongoClient(baseSettings.NinjaData.ConnectionString);
            var db = client.GetDatabase(baseSettings.NinjaData.DbName);
            _collection = db.GetCollection<TransactionInputMongoEntity>(TransactionInputMongoEntity.CollectionName);
        }


        public async Task InsertIfNotExists(IEnumerable<ITransactionInput> items)
        {
            var allIds = items.Select(p => p.Id);
            var existed = await _collection.AsQueryable().Where(p => allIds.Contains(p.Id)).Select(p => p.Id).ToListAsync();

            if (existed.Any())
            {
                await _log.WriteWarningAsync(nameof(TransactionInputRepository), nameof(InsertIfNotExists), 
                    existed.Take(5).ToJson(),
                    "Attempt To insert existed");
            }

            var itemsToInsert = items.Where(p => !existed.Contains(p.Id));

            if (itemsToInsert.Any())
            {
                await _collection.InsertManyAsync(itemsToInsert.Select(TransactionInputMongoEntity.Create));
            }
            
        }

        public async Task SetSpended(ISetSpendableOperationResult operationResult)
        {
            if (operationResult.Ok.Any() || operationResult.NotFound.Any())
            {
                var bulkOps = new List<WriteModel<TransactionInputMongoEntity>>();
                foreach (var input in operationResult.Ok)
                {
                    var id = TransactionOutputMongoEntity.GenerateId(input.Id);

                    var updateOneOp = new UpdateOneModel<TransactionInputMongoEntity>(
                        TransactionInputMongoEntity.Filter.EqId(id),
                        TransactionInputMongoEntity.Update.SetSpendedProcessed());

                    bulkOps.Add(updateOneOp);
                }

                foreach (var input in operationResult.NotFound)
                {
                    var id = TransactionOutputMongoEntity.GenerateId(input.Id);

                    var updateOneOp = new UpdateOneModel<TransactionInputMongoEntity>(
                        TransactionInputMongoEntity.Filter.EqId(id),
                        TransactionInputMongoEntity.Update.SetSpendedNotFound());

                    bulkOps.Add(updateOneOp);
                }

                await _collection.BulkWriteAsync(bulkOps);
            }
        }
    }

    public class TransactionInputMongoEntity: ITransactionInput
    {
        public const string CollectionName = "transaction-inputs";

        [BsonId]
        public string Id { get; set; }

        public string BlockId { get; set; }

        public int BlockHeight { get; set; }

        public string TransactionId { get; set; }

        public uint Index { get; set; }
        IInputTxIn ITransactionInput.TxIn => TxIn;

        public InputTxInMongoEntity TxIn { get; set; }

        public TransactionInputSpendProcessedInfoMongoEntity SpendProcessedInfo { get; set; }

        public static string GenerateId(string id)
        {
            return id;
        }

        public static TransactionInputMongoEntity Create(ITransactionInput source)
        {
            return new TransactionInputMongoEntity
            {
                Id = GenerateId(source.Id),
                BlockHeight = source.BlockHeight,
                BlockId = source.BlockId,
                Index = source.Index,
                TransactionId = source.TransactionId,
                TxIn = InputTxInMongoEntity.Create(source.TxIn),
                SpendProcessedInfo = TransactionInputSpendProcessedInfoMongoEntity.CreateWaiting()
            };
        }

        public static class Filter
        {
            public static FilterDefinition<TransactionInputMongoEntity> EqId(string id)
            {
                return Builders<TransactionInputMongoEntity>.Filter.Eq(p => p.Id, id);
            }
        }

        public static class Update
        {
            public static UpdateDefinition<TransactionInputMongoEntity> SetSpendedProcessed()
            {
                return Builders<TransactionInputMongoEntity>.Update.Set(p => p.SpendProcessedInfo,
                    TransactionInputSpendProcessedInfoMongoEntity.CreateOk());
            }

            public static UpdateDefinition<TransactionInputMongoEntity> SetSpendedNotFound()
            {
                return Builders<TransactionInputMongoEntity>.Update.Set(p => p.SpendProcessedInfo,
                    TransactionInputSpendProcessedInfoMongoEntity.CreateNotFound());
            }
        }
    }

    public class InputTxInMongoEntity: IInputTxIn
    {
        public string Id { get; set; }
        public string TransactionId { get; set; }

        public uint Index { get; set; }

        public static InputTxInMongoEntity Create(IInputTxIn source)
        {
            return new InputTxInMongoEntity
            {
                TransactionId = source.TransactionId,
                Index = source.Index,
                Id = source.Id
            };
        }
    }

    public enum SpendProcessedStatus
    {
        Waiting,
        Ok,
        NotFound
    }

    public class TransactionInputSpendProcessedInfoMongoEntity
    {
        public string Status { get; set; }

        public static TransactionInputSpendProcessedInfoMongoEntity CreateOk()
        {
            return new TransactionInputSpendProcessedInfoMongoEntity
            {
                Status = SpendProcessedStatus.Ok.ToString()
            };
        }


        public static TransactionInputSpendProcessedInfoMongoEntity CreateNotFound()
        {
            return new TransactionInputSpendProcessedInfoMongoEntity
            {
                Status = SpendProcessedStatus.NotFound.ToString()
            };
        }


        public static TransactionInputSpendProcessedInfoMongoEntity CreateWaiting()
        {
            return new TransactionInputSpendProcessedInfoMongoEntity
            {
                Status = SpendProcessedStatus.Waiting.ToString()
            };
        }
    }
}
