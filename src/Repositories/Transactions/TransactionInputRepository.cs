using System;
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
                TxIn = InputTxInMongoEntity.Create(source.InputTxIn)
            };
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
}
