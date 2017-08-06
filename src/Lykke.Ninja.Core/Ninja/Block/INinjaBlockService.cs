using System.Threading.Tasks;
using NBitcoin;
using QBitNinja.Client.Models;

namespace Core.Ninja.Block
{
    public interface INinjaBlockHeader
    {
        uint256 BlockId { get; }

        int BlockHeight { get; }
    }


    public interface INinjaBlockService
    {
        Task<INinjaBlockHeader> GetTip(bool withRetry = true);
        Task<GetBlockResponse> GetBlock(int height, bool withRetry = true);
        Task<GetBlockResponse> GetBlock(uint256 blockId, bool withRetry = true);

        Task<INinjaBlockHeader> GetBlockHeader(int height, bool withRetry = true);
        Task<INinjaBlockHeader> GetBlockHeader(uint256 blockId, bool withRetry = true);
        Task<INinjaBlockHeader> GetBlockHeader(string blockFeature, bool withRetry = true);
    }
}
