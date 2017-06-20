using System.Threading.Tasks;
using NBitcoin;
using QBitNinja.Client.Models;

namespace Core.Ninja.Block
{
    public interface INinjaBlockHeader
    {
        string BlockHash { get; }

        int BlockHeight { get; }
    }


    public interface INinjaBlockService
    {
        Task<INinjaBlockHeader> GetTip();
        Task<GetBlockResponse> GetBlock(int height);
        Task<GetBlockResponse> GetBlock(uint256 blockId);



        Task<INinjaBlockHeader> GetBlockHeader(int height);
        Task<INinjaBlockHeader> GetBlockHeader(uint256 blockId);
    }
}
