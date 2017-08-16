using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lykke.Ninja.Core.BlockStatus
{
    public interface IBlockStatus
    {
        int Height { get; }

        string BlockId { get; }

        BlockProcessingStatus ProcessingStatus { get; }
        DateTime QueuedAt { get; }
        DateTime StatusChangedAt { get; set; }
    }

    public class BlockStatus : IBlockStatus
    {
        public int Height { get; set; }
        public string BlockId { get; set; }
        public BlockProcessingStatus ProcessingStatus { get; set; }
        public DateTime QueuedAt { get; set; }
        public DateTime StatusChangedAt { get; set; }


        public static BlockStatus Create(int blockHeight,
            string blockId,
            BlockProcessingStatus blockProcessingStatus, 
            DateTime queuedAt,
            DateTime statusChangedAt)
        {
            return new BlockStatus
            {
                Height = blockHeight,
                BlockId = blockId,
                ProcessingStatus = blockProcessingStatus,
                QueuedAt = queuedAt,
                StatusChangedAt = statusChangedAt
            };
        }
    }

    public enum BlockProcessingStatus
    {
        Queued,
        Started,
        Done,
        Fail,
    }

    public interface IBlockStatusesRepository
    {
        Task<bool> Exists(string blockId);
        Task<IBlockStatus> GetLastQueuedBlock();
        Task<int> GetLastBlockHeight(BlockProcessingStatus status);
        Task<IBlockStatus> Get(string blockId);
        Task<IEnumerable<IBlockStatus>> GetAll(BlockProcessingStatus? status = null, 
            int? itemsToTake = null);


        Task<IEnumerable<int>> GetHeights(BlockProcessingStatus? status = null,
            int? itemsToTake = null);

        Task<long> Count(BlockProcessingStatus? status);
        Task Insert(IBlockStatus status);
        Task ChangeProcessingStatus(string blockId, BlockProcessingStatus status);
    }
}
