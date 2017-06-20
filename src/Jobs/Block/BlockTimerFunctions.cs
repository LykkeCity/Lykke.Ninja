using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Core.Ninja.Block;
using Lykke.JobTriggers.Triggers.Attributes;

namespace Jobs.Block
{
    public class BlockTimerFunctions
    {
        private readonly INinjaBlockService _ninjaBlockService;

        public BlockTimerFunctions(INinjaBlockService ninjaBlockService)
        {
            _ninjaBlockService = ninjaBlockService;
        }

        [TimerTrigger("00:00:30")]
        public async Task ScanNewBlocks()
        {
            var lastBlock = await _ninjaBlockService.GetTip();
        }
    }
}
