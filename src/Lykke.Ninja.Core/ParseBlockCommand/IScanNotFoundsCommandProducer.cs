using System.Threading.Tasks;

namespace Lykke.Ninja.Core.ParseBlockCommand
{
    public class ScanNotFoundsContext
    {
        
    }

    public interface IScanNotFoundsCommandProducer
    {
        //Scans blockchain -looking for missed blocks
        Task CreateScanNotFoundsCommand();
    }
}
