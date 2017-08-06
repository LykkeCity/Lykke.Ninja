using System.Collections.Generic;
using System.Threading.Tasks;
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
        private readonly BaseSettings _baseSettings;

        public AddressSummaryController(INinjaBlockService ninjaBlockService, 
            ITransactionOutputRepository outputRepository, 
            BaseSettings baseSettings)
        {
            _ninjaBlockService = ninjaBlockService;
            _outputRepository = outputRepository;
            _baseSettings = baseSettings;
        }

        [HttpGet("{address}/summary")]
        public async Task<AddressSummaryViewModel> Get(string address, 
            [FromQuery]string at = null, 
            [FromQuery]bool colored = false)
        {
            int? atBlockHeight = null;
            if (!string.IsNullOrEmpty(at))
            {
                var blockHeader = await _ninjaBlockService.GetBlockHeader(at);

                atBlockHeight = blockHeader?.BlockHeight;
            }

            var btcAddress = BitcoinAddressHelper.GetBitcoinAddress(address, 
                _baseSettings.UsedNetwork());

            var getTxCount = _outputRepository.GetTransactionsCount(btcAddress, 
                atBlockHeight);

            var getBtcAmount = _outputRepository.GetBtcAmountSummary(btcAddress, 
                atBlockHeight, 
                colored);

            var getbtcReceived = _outputRepository.GetBtcReceivedSummary(btcAddress, 
                atBlockHeight, 
                colored);

            var assetsReceiveds = _outputRepository.GetAssetsReceived(btcAddress, 
                atBlockHeight);

            var assetsAmounts = _outputRepository.GetAssetsAmount(btcAddress, 
                atBlockHeight);

            await Task.WhenAll(getTxCount, getBtcAmount, getbtcReceived, assetsReceiveds);

            return AddressSummaryViewModel.Create(getTxCount.Result, 
                getBtcAmount.Result, 
                getbtcReceived.Result, 
                assetsReceiveds.Result, 
                assetsAmounts.Result);
        }
    }
}
