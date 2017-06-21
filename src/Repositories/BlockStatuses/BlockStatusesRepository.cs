using System;
using System.Threading.Tasks;
using Core.BlockStatus;
using Core.Settings;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Repositories.BlockStatuses
{

    public class BlockStatusBuilder 
    {
        public static BlockStatus Create(BlockStatusMongoEntity source)
        {
            var inputOutputsGrabbedStatus = (InputOutputsGrabbedStatus)Enum.Parse(typeof(InputOutputsGrabbedStatus), source.InputOutputsGrabbedStatus);
            return BlockStatus.Create(source.Height, source.BlockId, inputOutputsGrabbedStatus);
        }
    }
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
            var result = await _collection.AsQueryable().OrderByDescending(p => p.Height).FirstOrDefaultAsync();

            if (result != null)
            {
                return BlockStatusBuilder.Create(result);
            }

            return null;
        }

        public async Task<IBlockStatus> Get(string blockId)
        {
            var result = await _collection.Find(BlockStatusMongoEntity.Filter.EqBlockId(blockId)).FirstOrDefaultAsync();

            if (result != null)
            {
                return BlockStatusBuilder.Create(result);
            }

            return null;
        }

        public Task Insert(IBlockStatus status)
        {
            var mongoEntity = BlockStatusMongoEntity.Create(status.BlockId, status.BlockHeight,
                status.InputOutputsGrabbedStatus);

            return _collection.InsertOneAsync(mongoEntity);
        }

        public async Task SetGrabbedStatus(string blockId, InputOutputsGrabbedStatus status)
        {
            await _collection.UpdateOneAsync(BlockStatusMongoEntity.Filter.EqBlockId(blockId),
                BlockStatusMongoEntity.Update.SetInputOutputsGrabbedStatus(status));
        }
    }

    public class BlockStatusMongoEntity
    {
        public const string CollectionName = "block-statuses";
        

        [BsonId]
        public string Id { get; set; }

        public int Height { get; set; }

        public string BlockId { get; set; }

        public string InputOutputsGrabbedStatus { get; set; }

        public static string GenerateId(string blockId)
        {
            return blockId;
        }

        public static BlockStatusMongoEntity Create(string blockId, int height, InputOutputsGrabbedStatus status)
        {
            return new BlockStatusMongoEntity
            {
                Id = GenerateId(blockId),
                BlockId = blockId,
                Height = height,
                InputOutputsGrabbedStatus = status.ToString()
            };
        }

        public static class Filter
        {
            public static FilterDefinition<BlockStatusMongoEntity> EqBlockId(string id)
            {
                return Builders<BlockStatusMongoEntity>.Filter.Eq(p => p.Id, BlockStatusMongoEntity.GenerateId(id));
            }
        }

        public static class Update
        {
            public static UpdateDefinition<BlockStatusMongoEntity> SetInputOutputsGrabbedStatus(InputOutputsGrabbedStatus status)
            {
                return Builders<BlockStatusMongoEntity>.Update.Set(p => p.InputOutputsGrabbedStatus, status.ToString());
            }
        }
    }
}
