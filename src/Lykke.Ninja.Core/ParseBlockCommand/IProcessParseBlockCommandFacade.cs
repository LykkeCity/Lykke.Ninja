using System.Threading.Tasks;

namespace Lykke.Ninja.Core.ParseBlockCommand
{
    public interface IProcessParseBlockCommandFacade
    {
        Task ProcessCommand(ParseBlockCommandContext context);
    }
}
