using System;
using System.Threading.Tasks;

namespace Core.BlockStatus
{

    public interface IBlockStatus
    {
        int Height { get; }

        string BlockId { get; }

        InputOutputsGrabbedStatus InputOutputsGrabbedStatus { get; }
        DateTime QueueddAt { get; }
    }

    public class BlockStatus : IBlockStatus
    {
        public int Height { get; set; }
        public string BlockId { get; set; }
        public InputOutputsGrabbedStatus InputOutputsGrabbedStatus { get; set; }
        public DateTime QueueddAt { get; set; }

        public static BlockStatus Create(int blockHeight,
            string blockId,
            InputOutputsGrabbedStatus inputOutputsGrabbedStatus, 
            DateTime queuedAt)
        {
            return new BlockStatus
            {
                Height = blockHeight,
                BlockId = blockId,
                InputOutputsGrabbedStatus = inputOutputsGrabbedStatus,
                QueueddAt = queuedAt
            };
        }
    }

    public enum InputOutputsGrabbedStatus
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
        Task<IBlockStatus> Get(string blockId);
        Task Insert(IBlockStatus status);
        Task SetGrabbedStatus(string blockId, InputOutputsGrabbedStatus status);
    }
}
