using System.Threading.Tasks;

namespace Core.ParseBlockCommand
{
    public interface IParseBlockCommandsService
    {
        Task ProduceParseBlockCommand(int blockHeight);

        Task<int> GetQueuedCommandCount();
    }
}
