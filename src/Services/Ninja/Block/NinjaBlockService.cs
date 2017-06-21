using System.Threading.Tasks;
using Core.Ninja.Block;
using NBitcoin;
using QBitNinja.Client;
using QBitNinja.Client.Models;

namespace Services.Ninja.Block
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

        public async Task<INinjaBlockHeader> GetTip()
        {
            var result = await Retry.Try(async () => await _ninjaClient.GetBlock(BlockFeature.Parse("tip"), headerOnly: true));

            return NinjaBlockHeader.Create(result.AdditionalInformation);
        }

        public Task<GetBlockResponse> GetBlock(int height)
        {
            return GetBlockInner(height.ToString());
        }

        public Task<GetBlockResponse> GetBlock(uint256 blockId)
        {
            return GetBlockInner(blockId.ToString());
        }

        public Task<INinjaBlockHeader> GetBlockHeader(int height)
        {
            return GetBlockHeaderInner(height.ToString());
        }

        public Task<INinjaBlockHeader> GetBlockHeader(uint256 blockId)
        {
            return GetBlockHeaderInner(blockId.ToString());
        }

        public async Task<INinjaBlockHeader> GetBlockHeaderInner(string blockFeature)
        {
            var result = await Retry.Try(async () => await _ninjaClient.GetBlock(BlockFeature.Parse(blockFeature), headerOnly: false));

            return NinjaBlockHeader.Create(result.AdditionalInformation);
        }


        private async Task<GetBlockResponse> GetBlockInner(string blockFeature)
        {
            return await Retry.Try(async () => await _ninjaClient.GetBlock(BlockFeature.Parse(blockFeature), headerOnly: false));
        }
    }
}
