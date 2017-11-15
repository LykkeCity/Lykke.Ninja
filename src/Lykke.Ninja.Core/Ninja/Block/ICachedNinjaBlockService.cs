using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using NBitcoin;

namespace Lykke.Ninja.Core.Ninja.Block
{
    public interface ICachedNinjaBlockService
    {
        Task<INinjaBlockHeader> GetBlockHeader(int height);
        Task<INinjaBlockHeader> GetBlockHeader(uint256 blockId);
        Task<INinjaBlockHeader> GetBlockHeader(string blockFeature);
        Task<INinjaBlockHeader> GetTip();
    }
}
