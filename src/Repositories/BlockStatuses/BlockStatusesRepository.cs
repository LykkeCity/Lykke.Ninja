﻿using System;
using System.Threading.Tasks;
using Core.BlockStatus;
using Core.Settings;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Repositories.BlockStatuses
{
    public class BlockStatusesRepository: IBlockStatusesRepository
    {
        private readonly IMongoCollection<BlockStatusMongoEntity> _collection;

        public BlockStatusesRepository(BaseSettings baseSettings)
        {
            var client = new MongoClient(baseSettings.NinjaData.ConnectionString);
            var db = client.GetDatabase(baseSettings.NinjaData.DbName);
            _collection = db.GetCollection<BlockStatusMongoEntity>(BlockStatusMongoEntity.CollectionName);
        }

        public async Task<bool> Exists(string blockId)
        {
            return await _collection.Find(BlockStatusMongoEntity.Filter.EqBlockId(blockId)).CountAsync() > 0;
        }

        public async Task<IBlockStatus> GetLastQueuedBlock()
        {
            return await _collection.AsQueryable().OrderByDescending(p => p.Height).FirstOrDefaultAsync();
        }

        public async Task<IBlockStatus> Get(string blockId)
        {
            return await _collection.Find(BlockStatusMongoEntity.Filter.EqBlockId(blockId)).FirstOrDefaultAsync();
        }

        public Task Insert(IBlockStatus status)
        {
            var mongoEntity = BlockStatusMongoEntity.Create(status);

            return _collection.InsertOneAsync(mongoEntity);
        }

        public async Task ChangeProcessingStatus(string blockId, BlockProcessingStatus status)
        {
            await _collection.UpdateOneAsync(BlockStatusMongoEntity.Filter.EqBlockId(blockId),
                BlockStatusMongoEntity.Update.SetInputOutputsGrabbedStatus(status));
        }
    }

    public class BlockStatusMongoEntity:IBlockStatus
    {
        public const string CollectionName = "block-statuses";
        

        [BsonId]
        public string Id { get; set; }

        public int Height { get; set; }
        
        public string BlockId { get; set; }

        BlockProcessingStatus IBlockStatus.ProcessingStatus => (BlockProcessingStatus)Enum.Parse(typeof(BlockProcessingStatus), ProcessingStatus);

        public DateTime QueuedAt { get; set; }
        public DateTime StatusChangedAt { get; set; }

        public string ProcessingStatus { get; set; }

        public static string GenerateId(string blockId)
        {
            return blockId;
        }

        public static BlockStatusMongoEntity Create(IBlockStatus source)
        {
            return new BlockStatusMongoEntity
            {
                Id = GenerateId(source.BlockId),
                BlockId = source.BlockId,
                Height = source.Height,
                ProcessingStatus = source.ProcessingStatus.ToString(),
                QueuedAt = source.QueuedAt,
                StatusChangedAt = source.StatusChangedAt
            };
        }

        public static class Filter
        {
            public static FilterDefinition<BlockStatusMongoEntity> EqBlockId(string id)
            {
                return Builders<BlockStatusMongoEntity>.Filter.Eq(p => p.Id, GenerateId(id));
            }
        }

        public static class Update
        {
            public static UpdateDefinition<BlockStatusMongoEntity> SetInputOutputsGrabbedStatus(BlockProcessingStatus status)
            {
                return Builders<BlockStatusMongoEntity>.Update.Set(p => p.ProcessingStatus, status.ToString())
                    .Set(p=>p.StatusChangedAt, DateTime.UtcNow);
            }
        }
    }
}