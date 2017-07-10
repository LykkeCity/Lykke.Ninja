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
using Repositories.Mongo;

namespace Repositories.Transactions
{
    public class TransactionInputRepository: ITransactionInputRepository
    {
        private readonly IMongoCollection<TransactionInputMongoEntity> _collection;
        private readonly ILog _log;

        private readonly Lazy<Task> _ensureQueryIndexes;
        private readonly Lazy<Task> _ensureInsertIndexes;

        public TransactionInputRepository(MongoSettings settings, 
            ILog log)
        {
            _log = log;
            var client = new MongoClient(settings.ConnectionString);
            var db = client.GetDatabase(settings.DataDbName);
            _collection = db.GetCollection<TransactionInputMongoEntity>(TransactionInputMongoEntity.CollectionName);

            _ensureQueryIndexes = new Lazy<Task>(SetQueryIndexes);
            _ensureInsertIndexes = new Lazy<Task>(SetInsertionIndexes);
        }


        public async Task InsertIfNotExists(IEnumerable<ITransactionInput> items)
        {
            await EnsureInsertionIndexes();

            var allIds = items.Select(p => p.Id);
            var existed = await _collection.AsQueryable().Where(p => allIds.Contains(p.Id)).Select(p => p.Id).ToListAsync();

            var itemsToInsert = items.Where(p => !existed.Contains(p.Id)).ToList();

            await Insert(itemsToInsert);

        }

        public async Task InsertIfNotExists(IEnumerable<ITransactionInput> items, int blockHeight)
        {
            await EnsureInsertionIndexes();

            var existed = await _collection.AsQueryable().Where(p => p.BlockHeight == blockHeight).Select(p => p.Id).ToListAsync();

            var itemsToInsert = items.Where(p => !existed.Contains(p.Id)).ToList();
            try
            {

                await Insert(itemsToInsert);
            }
            catch (Exception e) // todo catch mongoDuplicate exception
            {
                await _log.WriteInfoAsync(nameof(TransactionOutputRepository), nameof(InsertIfNotExists), blockHeight.ToString(), e.ToString());

                await InsertIfNotExists(items);
            }
        }

        private async Task Insert(IEnumerable<ITransactionInput> items)
        {
            await EnsureInsertionIndexes();

            if (items.Any())
            {
                await _collection.InsertManyAsync(items.Select(TransactionInputMongoEntity.Create), new InsertManyOptions { IsOrdered = false });
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

        public async Task<IEnumerable<ITransactionInput>> Get(SpendProcessedStatus status, int? itemsToTake = null)
        {
            await EnsureQueryIndexes();
            var query = _collection.Find(TransactionInputMongoEntity.Filter.EqStatus(status))
                .Sort(new SortDefinitionBuilder<TransactionInputMongoEntity>().Ascending(p => p.BlockHeight));

            if (itemsToTake != null)
            {
                query = query.Limit(itemsToTake);
            }

            return await query.ToListAsync();
        }

        public async Task<long> Count(SpendProcessedStatus status)
        {
            await EnsureQueryIndexes();

            return await _collection.Find(TransactionInputMongoEntity.Filter.EqStatus(status)).CountAsync();
        }



        #region  indexes

        private async Task EnsureInsertionIndexes()
        {
            await _ensureInsertIndexes.Value;
        }

        private async Task EnsureQueryIndexes()
        {
            await _ensureQueryIndexes.Value;
        }

        private async Task SetInsertionIndexes()
        {
            await _log.WriteInfoAsync(nameof(TransactionInputRepository), nameof(SetInsertionIndexes), null, "Started");
            
            var setIndexes = new[]
            {
                SetHeightIndex(),
                SetIdIndex()
            };

            await Task.WhenAll(setIndexes);

            await _log.WriteInfoAsync(nameof(TransactionInputRepository), nameof(SetInsertionIndexes), null, "Done");
        }


        private async Task SetQueryIndexes()
        {
            await _log.WriteInfoAsync(nameof(TransactionInputRepository), nameof(SetQueryIndexes), null, "Started");

            var setIndexes = new[]
            {
                SetHeightIndex(),
                SetStatusIndex()
            };

            await Task.WhenAll(setIndexes);

            await _log.WriteInfoAsync(nameof(TransactionInputRepository), nameof(SetQueryIndexes), null, "Done");
        }

        #region Single

        private async Task SetHeightIndex()
        {
            var blockHeightIndex = Builders<TransactionInputMongoEntity>.IndexKeys.Descending(p => p.BlockHeight);
            await _collection.Indexes.CreateOneAsync(blockHeightIndex, new CreateIndexOptions { Background = false });
        }

        private async Task SetIdIndex()
        {
            var idIndex = Builders<TransactionInputMongoEntity>.IndexKeys.Descending(p => p.Id);
            await _collection.Indexes.CreateOneAsync(idIndex, new CreateIndexOptions { Unique = true });
        }

        private async Task SetStatusIndex()
        {
            var statusIndex = Builders<TransactionInputMongoEntity>.IndexKeys.Descending(p => p.SpendProcessedInfo.Status);
            await _collection.Indexes.CreateOneAsync(statusIndex, new CreateIndexOptions { Background = false });
        }
        
        #endregion
        #endregion


    }

    public class TransactionInputMongoEntity: ITransactionInput
    {
        public const string CollectionName = "transaction-inputs";

        [BsonId(IdGenerator = typeof(ObjectIdGenerator))]
        public ObjectId _id { get; set; }


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

            public static FilterDefinition<TransactionInputMongoEntity> EqStatus(SpendProcessedStatus status)
            {
                return Builders<TransactionInputMongoEntity>.Filter.Eq(p => p.SpendProcessedInfo.Status, status.ToString());
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
