using System.Threading.Tasks;

namespace Core.BlockStatus
{

    public interface IBlockStatus
    {
        int BlockHeight { get; }

        string BlockId { get; }
        InputOutputsGrabbedStatus InputOutputsGrabbedStatus { get; }
    }

    public class BlockStatus : IBlockStatus
    {
        public int BlockHeight { get; set; }
        public string BlockId { get; set; }
        public InputOutputsGrabbedStatus InputOutputsGrabbedStatus { get; set; }

        public static BlockStatus Create(int blockHeight, string blockId,
            InputOutputsGrabbedStatus inputOutputsGrabbedStatus)
        {
            return new BlockStatus
            {
                BlockHeight = blockHeight,
                BlockId = blockId,
                InputOutputsGrabbedStatus = inputOutputsGrabbedStatus
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
