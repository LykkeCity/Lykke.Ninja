using System.Threading.Tasks;
using QBitNinja.Client.Models;

namespace Core.Block
{
    public interface IBlockService
    {
        Task Parse(GetBlockResponse block);
    }
}
