using System.Threading.Tasks;
using Lykke.Ninja.Core.Ninja.Block;
using NBitcoin;
using QBitNinja.Client;
using QBitNinja.Client.Models;

namespace Lykke.Ninja.Services.Ninja.Block
{
    public class NinjaBlockHeader : INinjaBlockHeader
    {
        public uint256 BlockId { get; set; }
        public int BlockHeight { get; set; }

        public static NinjaBlockHeader Create(BlockInformation source)
        {
            return new NinjaBlockHeader
            {
                BlockId = source.BlockId,
                BlockHeight = source.Height
            };
        }
    }

    public class NinjaBlockService:INinjaBlockService
    {
        private readonly QBitNinjaClient _ninjaClient;

        public NinjaBlockService(QBitNinjaClient ninjaClient)
        {
            _ninjaClient = ninjaClient;
        }

        public async Task<INinjaBlockHeader> GetTip(bool withRetry = true)
        {
            return await GetBlockHeader("tip", withRetry);
        }

        public Task<GetBlockResponse> GetBlock(int height, bool withRetry = true)
        {
            return GetBlockInner(height.ToString(), withRetry);
        }

        public Task<GetBlockResponse> GetBlock(uint256 blockId, bool withRetry = true)
        {
            return GetBlockInner(blockId.ToString(), withRetry);
        }

        public Task<INinjaBlockHeader> GetBlockHeader(int height, bool withRetry = true)
        {
            return GetBlockHeader(height.ToString(), withRetry);
        }

        public Task<INinjaBlockHeader> GetBlockHeader(uint256 blockId, bool withRetry = true)
        {
            return GetBlockHeader(blockId.ToString(), withRetry);
        }

        public async Task<INinjaBlockHeader> GetBlockHeader(string blockFeature, bool withRetry = true)
        {
            GetBlockResponse result;

            if (withRetry)
            {
                result = await Retry.Try(async () => await _ninjaClient.GetBlock(BlockFeature.Parse(blockFeature), headerOnly: true));
            }
            else
            {
                result = await _ninjaClient.GetBlock(BlockFeature.Parse(blockFeature), headerOnly: true);
            }

            return NinjaBlockHeader.Create(result.AdditionalInformation);
        }


        private async Task<GetBlockResponse> GetBlockInner(string blockFeature, bool withRetry = true)
        {
            if (withRetry)
            {

                return await Retry.Try(async () => await _ninjaClient.GetBlock(BlockFeature.Parse(blockFeature), headerOnly: false));
            }

            return await _ninjaClient.GetBlock(BlockFeature.Parse(blockFeature), headerOnly: false);
        }
    }
}
