using System;
using System.Collections.Generic;
using Common.Cache;
using System.Threading.Tasks;
using Lykke.Ninja.Core.Ninja.Block;
using NBitcoin;

namespace Lykke.Ninja.Services.Ninja.Block
{
    public class CachedNinjaBlockService: ICachedNinjaBlockService
    {
        private readonly ICacheManager _cache;
        private readonly INinjaBlockService _ninjaBlockService;

        public CachedNinjaBlockService(ICacheManager cache, INinjaBlockService ninjaBlockService)
        {
            _cache = cache;
            _ninjaBlockService = ninjaBlockService;
        }

        public Task<INinjaBlockHeader> GetBlockHeader(int height)
        {
            return Get(height.ToString());
        }

        public Task<INinjaBlockHeader> GetBlockHeader(uint256 blockId)
        {
            return Get(blockId.ToString());
        }

        public Task<INinjaBlockHeader> GetBlockHeader(string blockFeature)
        {
            return Get(blockFeature);
        }

        public Task<INinjaBlockHeader> GetTip()
        {
            return Get("tip", 1);
        }


        private async Task<INinjaBlockHeader> Get(string blockFeature, int cacheTimeMinutes = 10)
        {
            if (_cache.IsSet(blockFeature))
            {
                return _cache.Get<INinjaBlockHeader>(blockFeature);
            }

            var data = await _ninjaBlockService.GetBlockHeader(blockFeature, withRetry:false);

            if (data != null)
            {
                _cache.Set(blockFeature, data, cacheTimeMinutes);
            }

            return data;
        }
        
    }
}
