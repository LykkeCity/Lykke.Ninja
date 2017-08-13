using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Lykke.Ninja.Core.Settings;
using Lykke.Ninja.Core.Transaction;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Lykke.Ninja.Repositories.Mongo;

namespace Lykke.Ninja.Repositories.Transactions
{
    public class TransactionInputRepository: ITransactionInputRepository
    {
        private readonly IMongoCollection<TransactionInputMongoEntity> _collection;
        private readonly ILog _log;
        private readonly IConsole _console;

        private readonly Lazy<Task> _ensureQueryIndexesLocker;
        private readonly Lazy<Task> _ensureInsertIndexesLocker;
        private readonly Lazy<Task> _ensureUpdateIndexesLocker;

        private readonly BaseSettings _baseSettings;

        public TransactionInputRepository(MongoSettings settings, 
            ILog log, 
            IConsole console, 
            BaseSettings baseSettings)
        {
            _log = log;
            _console = console;
            _baseSettings = baseSettings;
            var client = new MongoClient(settings.ConnectionString);
            var db = client.GetDatabase(settings.DataDbName);
            _collection = db.GetCollection<TransactionInputMongoEntity>(TransactionInputMongoEntity.CollectionName);

            _ensureQueryIndexesLocker = new Lazy<Task>(SetQueryIndexes);
            _ensureInsertIndexesLocker = new Lazy<Task>(SetInsertionIndexes);
            _ensureUpdateIndexesLocker = new Lazy<Task>(SetUpdateIndexes);
        }

        private void WriteConsole(int blockHeight, string message)
        {
            _console.WriteLine($"{nameof(TransactionInputRepository)} Block Height:{blockHeight} {message}");
        }

        private void WriteConsole(string message)
        {
            _console.WriteLine($"{nameof(TransactionInputRepository)}  {message}");
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

            WriteConsole(blockHeight, "Retrieving existed started");
            var existed = await _collection.AsQueryable().Where(p => p.BlockHeight == blockHeight).Select(p => p.Id).ToListAsync();
            WriteConsole(blockHeight, "Retrieving existed done");

            var itemsToInsert = items.Where(p => !existed.Contains(p.Id)).ToList();
            try
            {
                WriteConsole(blockHeight, "Insert started");
                await Insert(itemsToInsert);
                WriteConsole(blockHeight, "Insert done");
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
            await EnsureUpdateIndexes();
            if (operationResult.Ok.Any())
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
                
                WriteConsole($"Update Ok started {bulkOps.Count}");
                await _collection.BulkWriteAsync(bulkOps, new BulkWriteOptions { IsOrdered = false });
                WriteConsole($"Update Ok done {bulkOps.Count}");
            }
            
            if (operationResult.NotFound.Any())
            {
                var bulkOps = new List<WriteModel<TransactionInputMongoEntity>>();

                foreach (var input in operationResult.NotFound)
                {
                    var id = TransactionOutputMongoEntity.GenerateId(input.Id);

                    var updateOneOp = new UpdateOneModel<TransactionInputMongoEntity>(
                        TransactionInputMongoEntity.Filter.EqId(id),
                        TransactionInputMongoEntity.Update.SetSpendedNotFound());

                    bulkOps.Add(updateOneOp);
                }

                WriteConsole($"Update NotFound started {bulkOps.Count}");
                await _collection.BulkWriteAsync(bulkOps, new BulkWriteOptions { IsOrdered = false });
                WriteConsole($"Update NotFound done {bulkOps.Count}");
            }
        }

        public async Task<IEnumerable<ITransactionInput>> Get(SpendProcessedStatus status, int itemsToTake, int? itemsToSkip)
        {
            await EnsureQueryIndexes();
            
            var query = _collection.Find(TransactionInputMongoEntity.Filter.EqStatus(status));

            if (itemsToSkip != null)
            {
                query = query.Skip(itemsToSkip);
            }

            query = query.Limit(itemsToTake);

            _console.WriteLine($"{nameof(TransactionInputRepository)}.{nameof(Get)} Started");
            var result = await query.ToListAsync();
            _console.WriteLine($"{nameof(TransactionInputRepository)}.{nameof(Get)} Done");

            return result;
        }

        public async Task<long> Count(SpendProcessedStatus status)
        {
            await EnsureQueryIndexes();

            return await _collection.Find(TransactionInputMongoEntity.Filter.EqStatus(status)).CountAsync();
        }
        
        #region  indexes

        private async Task EnsureInsertionIndexes()
        {
            await _ensureInsertIndexesLocker.Value;
        }

        private async Task EnsureQueryIndexes()
        {
            await _ensureQueryIndexesLocker.Value;
        }


        private async Task EnsureUpdateIndexes()
        {
            await _ensureUpdateIndexesLocker.Value;
        }


        private async Task SetInsertionIndexes()
        {
            await _log.WriteInfoAsync(nameof(TransactionInputRepository), nameof(SetInsertionIndexes), null, "Started");
            
            var setIndexes = new List<Task>
            {
                SetHeightIndex(),
            };


            if (_baseSettings.InitialParser?.SetInputIdIndex ?? true)
            {
                setIndexes.Add(SetIdIndex());
            }

            await Task.WhenAll(setIndexes);

            await _log.WriteInfoAsync(nameof(TransactionInputRepository), nameof(SetInsertionIndexes), null, "Done");
        }

        private async Task SetUpdateIndexes()
        {
            await _log.WriteInfoAsync(nameof(TransactionInputRepository), nameof(SetUpdateIndexes), null, "Started");

            var setIndexes = new List<Task>
            {
                SetIdIndex(),
            };
            
            await Task.WhenAll(setIndexes);

            await _log.WriteInfoAsync(nameof(TransactionInputRepository), nameof(SetUpdateIndexes), null, "Done");
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
            var statusIndex = Builders<TransactionInputMongoEntity>.IndexKeys.Descending(p => p.Status);
            await _collection.Indexes.CreateOneAsync(statusIndex, new CreateIndexOptions { Background = false });
        }
        
        #endregion

        #endregion
    }

    [BsonIgnoreExtraElements]
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

        [BsonElement("stt")]
        public int Status { get; set; }
        IInputTxIn ITransactionInput.TxIn => TxIn;

        public InputTxInMongoEntity TxIn { get; set; }
        
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
                Status = (int)SpendProcessedStatus.Waiting
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
                return Builders<TransactionInputMongoEntity>.Filter.Eq(p => p.Status, (int) status);
            }
        }

        public static class Update
        {
            public static UpdateDefinition<TransactionInputMongoEntity> SetSpendedProcessed()
            {
                return Builders<TransactionInputMongoEntity>.Update.Set(p => p.Status,
                    (int)SpendProcessedStatus.Ok);
            }

            public static UpdateDefinition<TransactionInputMongoEntity> SetSpendedNotFound()
            {
                return Builders<TransactionInputMongoEntity>.Update.Set(p => p.Status,
                    (int)SpendProcessedStatus.NotFound);
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
}
