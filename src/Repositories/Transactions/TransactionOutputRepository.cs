using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core;
using Core.Settings;
using Core.Transaction;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace Repositories.Transactions
{
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



            await _collection.InsertManyAsync(outputs.Select(TransactionOutputMongoEntity.Create));
            //try
            //{
            //    await _collection.InsertManyAsync(outputs.Select(TransactionOutputMongoEntity.Create));
            //}
            //catch (Exception e)
            //{
            //    var bl = outputs.First().BlockId;
            //    var insert = outputs.Select(TransactionOutputMongoEntity.Create);

            //    var current = _collection.AsQueryable().Where(p => p.BlockId == bl).ToList();
            //}



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

        public uint OutputIndex { get; set; }

        public long BtcSatoshiAmount { get; set; }

        public string DestinationAddress { get; set; }

        public static string GenerateId(string transactionId, uint index)
        {
            return $"{transactionId}_{index}";
        }

        public static TransactionOutputMongoEntity Create(ITransactionOutput source)
        {
            return new TransactionOutputMongoEntity
            {
                Id = GenerateId(source.TransactionId, source.OutputIndex),
                BlockHeight = source.BlockHeight,
                BlockId = source.BlockId,
                OutputIndex = source.OutputIndex,
                TransactionId = source.TransactionId,
                BtcSatoshiAmount = source.BtcSatoshiAmount,
                DestinationAddress = source.DestinationAddress
            };
        }
    }
}
