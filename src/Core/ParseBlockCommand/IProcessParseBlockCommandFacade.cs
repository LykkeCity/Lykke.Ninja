using System.Threading.Tasks;

namespace Core.ParseBlockCommand
{
    public interface IProcessParseBlockCommandFacade
    {
        Task ProcessCommand(ParseBlockCommandContext context);
    }
}
