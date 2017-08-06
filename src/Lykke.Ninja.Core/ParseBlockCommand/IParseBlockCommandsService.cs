using System.Threading.Tasks;

namespace Lykke.Ninja.Core.ParseBlockCommand
{
    public interface IParseBlockCommandsService
    {
        Task ProduceParseBlockCommand(int blockHeight);

        Task<int> GetQueuedCommandCount();
    }
}
