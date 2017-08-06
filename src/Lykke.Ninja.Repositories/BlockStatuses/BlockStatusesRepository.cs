using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Ninja.Core.BlockStatus;
using Lykke.Ninja.Core.Settings;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Lykke.Ninja.Repositories.Mongo;

namespace Lykke.Ninja.Repositories.BlockStatuses
{
    public class BlockStatusesRepository: IBlockStatusesRepository
    {
        private readonly IMongoCollection<BlockStatusMongoEntity> _collection;

        private readonly Lazy<Task> _ensureQueryIndexes;
        private readonly Lazy<Task> _ensureInsertIndexes;

        private readonly ILog _log;
        public BlockStatusesRepository(MongoSettings settings, ILog log)
        {
            _log = log;
            var client = new MongoClient(settings.ConnectionString);
            var db = client.GetDatabase(settings.DataDbName);
            _collection = db.GetCollection<BlockStatusMongoEntity>(BlockStatusMongoEntity.CollectionName);

            _ensureQueryIndexes = new Lazy<Task>(SetQueryIndexes);
            _ensureInsertIndexes = new Lazy<Task>(SetInsertionIndexes);
        }

        public async Task<bool> Exists(string blockId)
        {
            await EnsureQueryIndexes();
            return await _collection.Find(BlockStatusMongoEntity.Filter.EqBlockId(blockId)).CountAsync() > 0;
        }

        public async Task<IBlockStatus> GetLastQueuedBlock()
        {
            await EnsureQueryIndexes();
            return await _collection.AsQueryable().OrderByDescending(p => p.Height).FirstOrDefaultAsync();
        }

        public async Task<IBlockStatus> Get(string blockId)
        {
            await EnsureQueryIndexes();
            return await _collection.Find(BlockStatusMongoEntity.Filter.EqBlockId(blockId)).FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<IBlockStatus>> GetAll(BlockProcessingStatus? status, int? itemsToTake)
        {
            await EnsureQueryIndexes();
            var query = _collection.AsQueryable();
            if (status != null)
            {
                query = query.Where(p => p.ProcessingStatus == status.ToString());
            }

            if (itemsToTake != null)
            {
                query = query.Take(itemsToTake.Value);
            }

            return await query.OrderBy(p => p.Height).ToListAsync();
        }

        public async Task<IEnumerable<int>> GetHeights(BlockProcessingStatus? status = null, int? itemsToTake = null)
        {
            await EnsureQueryIndexes();
            var query = _collection.AsQueryable();
            if (status != null)
            {
                query = query.Where(p => p.ProcessingStatus == status.ToString());
            }

            if (itemsToTake != null)
            {
                query = query.Take(itemsToTake.Value);
            }

            return await query.OrderByDescending(p => p.Height).Select(p=>p.Height).ToListAsync();
        }

        public async Task<long> Count(BlockProcessingStatus? status)
        {
            var query = _collection.AsQueryable();
            if (status != null)
            {
                query = query.Where(p => p.ProcessingStatus == status.ToString());
            }

            return await query.CountAsync();
        }

        public async Task Insert(IBlockStatus status)
        {
            await EnsureInsertionIndexes();

            var mongoEntity = BlockStatusMongoEntity.Create(status);

            await _collection.InsertOneAsync(mongoEntity);
        }

        public async Task ChangeProcessingStatus(string blockId, BlockProcessingStatus status)
        {
            await EnsureInsertionIndexes();
            await _collection.UpdateOneAsync(BlockStatusMongoEntity.Filter.EqBlockId(blockId),
                BlockStatusMongoEntity.Update.SetInputOutputsGrabbedStatus(status));
        }
        
        #region Indexes

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
            await _log.WriteInfoAsync(nameof(BlockStatusesRepository), nameof(SetInsertionIndexes), null, "Started");

            var setIndexes = new[]
            {
                SetIdIndex()
            };

            await Task.WhenAll(setIndexes);

            await _log.WriteInfoAsync(nameof(BlockStatusesRepository), nameof(SetInsertionIndexes), null, "Done");
        }

        private async Task SetQueryIndexes()
        {
            await _log.WriteInfoAsync(nameof(BlockStatusesRepository), nameof(SetQueryIndexes), null, "Started");

            var setIndexes = new[]
            {
                SetHeightIndex(),
                SetStatusIndex(),
                SetIdIndex()
            };

            await Task.WhenAll(setIndexes);

            await _log.WriteInfoAsync(nameof(BlockStatusesRepository), nameof(SetQueryIndexes), null, "Done");
        }


        private async Task SetHeightIndex()
        {
            var blockHeightIndex = Builders<BlockStatusMongoEntity>.IndexKeys.Descending(p => p.Height);
            await _collection.Indexes.CreateOneAsync(blockHeightIndex, new CreateIndexOptions { Background = true });
        }

        private async Task SetIdIndex()
        {
            var idIndex = Builders<BlockStatusMongoEntity>.IndexKeys.Descending(p => p.Id);
            await _collection.Indexes.CreateOneAsync(idIndex, new CreateIndexOptions { Unique = true });
        }

        private async Task SetStatusIndex()
        {
            var statusIndex = Builders<BlockStatusMongoEntity>.IndexKeys.Descending(p => p.ProcessingStatus);
            await _collection.Indexes.CreateOneAsync(statusIndex, new CreateIndexOptions { Background = true });
        }

        #endregion
    }

    public class BlockStatusMongoEntity:IBlockStatus
    {
        public const string CollectionName = "block-statuses";

        [BsonId(IdGenerator = typeof(ObjectIdGenerator))]
        public ObjectId _id { get; set; }
        
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
