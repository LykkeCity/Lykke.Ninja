using System.Threading.Tasks;

namespace Core.ParseBlockCommand
{
    public class ParseBlockCommandContext
    {
        public string BlockId { get; set; }

        public int BlockHeight { get; set; }
    }


    public interface IParseBlockCommandProducer
    {
        Task CreateParseBlockCommand(string blockId, int blockHeight);
        Task<int> GetQueuedCommandCount();
    }
}
