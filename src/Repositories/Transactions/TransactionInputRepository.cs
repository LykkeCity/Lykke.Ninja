﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.Settings;
using Core.Transaction;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace Repositories.Transactions
{
    public class TransactionInputRepository: ITransactionInputRepository
    {
        private readonly IMongoCollection<TransactionInputMongoEntity> _collection;

        public TransactionInputRepository(BaseSettings baseSettings)
        {
            var client = new MongoClient(baseSettings.NinjaData.ConnectionString);
            var db = client.GetDatabase(baseSettings.NinjaData.DbName);
            _collection = db.GetCollection<TransactionInputMongoEntity>(TransactionInputMongoEntity.CollectionName);
        }


        public async Task Insert(IEnumerable<ITransactionInput> inputs)
        {
            if (inputs.Any())
            {
                await _collection.InsertManyAsync(inputs.Select(TransactionInputMongoEntity.Create));
            }
        }

        public async Task SetSpendedProcessedBulk(ISetSpendableOperationResult operationResult)
        {
            if (operationResult.Ok.Any() || operationResult.NotFound.Any())
            {
                var bulkOps = new List<WriteModel<TransactionInputMongoEntity>>();
                foreach (var input in operationResult.Ok)
                {
                    var id = TransactionOutputMongoEntity.GenerateId(input.TransactionId, input.Index);

                    var updateOneOp = new UpdateOneModel<TransactionInputMongoEntity>(
                        TransactionInputMongoEntity.Filter.EqId(id),
                        TransactionInputMongoEntity.Update.SetSpendedProcessed());

                    bulkOps.Add(updateOneOp);
                }

                foreach (var input in operationResult.Ok)
                {
                    var id = TransactionOutputMongoEntity.GenerateId(input.TransactionId, input.Index);

                    var updateOneOp = new UpdateOneModel<TransactionInputMongoEntity>(
                        TransactionInputMongoEntity.Filter.EqId(id),
                        TransactionInputMongoEntity.Update.SetSpendedNotFound());

                    bulkOps.Add(updateOneOp);
                }

                await _collection.BulkWriteAsync(bulkOps);
            }
        }
    }

    public class TransactionInputMongoEntity
    {
        public const string CollectionName = "transaction-inputs";

        [BsonId]
        public string Id { get; set; }

        public string BlockId { get; set; }

        public int BlockHeight { get; set; }

        public string TransactionId { get; set; }

        public uint Index { get; set; }

        public InputTxInMongoEntity TxIn { get; set; }

        public TransactionInputSpendProcessedInfoMongoEntity SpendProcessedInfo { get; set; }

        public static string GenerateId(string transactionId, uint index)
        {
            return $"{transactionId}_{index}";
        }

        public static TransactionInputMongoEntity Create(ITransactionInput source)
        {
            return new TransactionInputMongoEntity
            {
                Id = GenerateId(source.TransactionId, source.Index),
                BlockHeight = source.BlockHeight,
                BlockId = source.BlockId,
                Index = source.Index,
                TransactionId = source.TransactionId,
                TxIn = InputTxInMongoEntity.Create(source.InputTxIn),
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

    public class InputTxInMongoEntity
    {
        public string TransactionId { get; set; }

        public uint Index { get; set; }

        public static InputTxInMongoEntity Create(IInputTxIn source)
        {
            return new InputTxInMongoEntity
            {
                TransactionId = source.TransactionId,
                Index = source.Index
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
