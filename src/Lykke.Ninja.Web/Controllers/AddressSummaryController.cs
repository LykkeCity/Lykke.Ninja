using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Ninja.Core.Block;
using Lykke.Ninja.Core.BlockStatus;
using Lykke.Ninja.Core.Ninja.Block;
using Lykke.Ninja.Core.Settings;
using Lykke.Ninja.Core.Transaction;
using Microsoft.AspNetCore.Mvc;
using Lykke.Ninja.Services.Ninja;
using Lykke.Ninja.Web.Models;

namespace Lykke.Ninja.Web.Controllers
{
    [Route("balances")]
    public class AddressSummaryController : Controller
    {
        private readonly INinjaBlockService _ninjaBlockService;
        private readonly ITransactionOutputRepository _outputRepository;
        private readonly IBlockStatusesRepository _blockStatusesRepository;
        private readonly BaseSettings _baseSettings;

        public AddressSummaryController(INinjaBlockService ninjaBlockService, 
            ITransactionOutputRepository outputRepository, 
            BaseSettings baseSettings,
            IBlockStatusesRepository blockStatusesRepository)
        {
            _ninjaBlockService = ninjaBlockService;
            _outputRepository = outputRepository;
            _baseSettings = baseSettings;
            _blockStatusesRepository = blockStatusesRepository;
        }

        [HttpGet("{address}/summary")]
        public async Task<AddressSummaryViewModel> Get(string address, 
            [FromQuery]string at = null, 
            [FromQuery]bool colored = false)
        {
            var getAtBlockHeight = GetBlockHeight(at);
            
            var btcAddress = BitcoinAddressHelper.GetBitcoinAddress(address, 
                _baseSettings.UsedNetwork());

            await getAtBlockHeight;

            var getTxCount = _outputRepository.GetTransactionsCount(btcAddress, 
                getAtBlockHeight.Result);

            var getBtcAmount = _outputRepository.GetBtcAmountSummary(btcAddress, 
                getAtBlockHeight.Result, 
                colored);

            var getbtcReceived = _outputRepository.GetBtcReceivedSummary(btcAddress, 
                getAtBlockHeight.Result, 
                colored);

            Task<IDictionary<string, long>> assetsReceiveds;
            Task<IDictionary<string, long>> assetsAmounts;
            if (colored)
            {
                assetsReceiveds = _outputRepository.GetAssetsReceived(btcAddress,
                    getAtBlockHeight.Result);

                assetsAmounts = _outputRepository.GetAssetsAmount(btcAddress,
                    getAtBlockHeight.Result);
            }
            else
            {
                IDictionary<string, long> emptyResult = new Dictionary<string, long>();
                assetsReceiveds = Task.FromResult(emptyResult);
                assetsAmounts = Task.FromResult(emptyResult);
            }
            

            await Task.WhenAll(getTxCount, getBtcAmount, getbtcReceived, assetsReceiveds);

            return AddressSummaryViewModel.Create(getTxCount.Result, 
                getBtcAmount.Result, 
                getbtcReceived.Result, 
                assetsReceiveds.Result, 
                assetsAmounts.Result);
        }

        private async Task<int?> GetBlockHeight(string descriptor)
        {
            if (string.IsNullOrEmpty(descriptor) || NinjaBlockHelper.IsTopBlock(descriptor))
            {
                return await _blockStatusesRepository.GetLastBlockHeight(BlockProcessingStatus.Done);
            }

            int parsedValue;

            if (int.TryParse(descriptor, out parsedValue))
            {
                return parsedValue;
            }


            var blockHeader = await _ninjaBlockService.GetBlockHeader(descriptor, withRetry: false);

            return blockHeader?.BlockHeight;
        }
    }
}
