using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Ninja.Core.Ninja.Block;
using Lykke.Ninja.Core.Transaction;
using Lykke.Ninja.Web.Models;
using Microsoft.AspNetCore.Mvc;
using NBitcoin;

namespace Lykke.Ninja.Web.Controllers
{
    public class BlockTransactionsController: Controller
    {
        private readonly ITransactionOutputRepository _transactionOutputRepository;
        private readonly ICachedNinjaBlockService _ninjaBlockService;
        private readonly Network _network;

        public BlockTransactionsController(ITransactionOutputRepository transactionOutputRepository,
            ICachedNinjaBlockService ninjaBlockService, 
            Network network)
        {
            _transactionOutputRepository = transactionOutputRepository;
            _ninjaBlockService = ninjaBlockService;
            _network = network;
        }

        [HttpGet("blocks/{block}/transactions")]
        public async Task<TransactionsViewModel> GetByBlock(string block, [FromQuery] bool colored = true)
        {
            var getTip = _ninjaBlockService.GetTip();
            var blockHeight = await GetBlockHeight(block);
            
            var getSpended = _transactionOutputRepository.GetSpendedByBlock(blockHeight);
            var getReceived = _transactionOutputRepository.GetReceivedByBlock(blockHeight);

            await Task.WhenAll(getTip, getSpended, getReceived);

            return TransactionsViewModel.Create(getTip.Result, _network, colored, getSpended.Result, getReceived.Result, showFees: true, showAmount: false);
        }

        private async Task<int> GetBlockHeight(string blockFeature)
        {
            int result;
            if (int.TryParse(blockFeature, out result))
            {
                return result;
            }
            
            var blockHeader = await _ninjaBlockService.GetBlockHeader(blockFeature);

            if (blockHeader != null)
            {
                return blockHeader.BlockHeight;
            }

            throw new Exception($"{blockFeature} not found");
        }
    }
}
