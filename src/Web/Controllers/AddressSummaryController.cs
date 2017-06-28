using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Ninja.Block;
using Core.Settings;
using Core.Transaction;
using Microsoft.AspNetCore.Mvc;
using Services.Ninja;
using Services.Settings;
using Web.Models;

namespace Web.Controllers
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
        public async Task<AddressSummaryViewModel> Get(string address, [FromQuery]string at = null, [FromQuery]bool colored = false)
        {
            int? atBlockHeight = null;
            if (!string.IsNullOrEmpty(at))
            {
                var blockHeader = await _ninjaBlockService.GetBlockHeader(at);

                atBlockHeight = blockHeader?.BlockHeight;
            }

            var btcAddress = BitcoinAddressHelper.GetBitcoinAddress(address, _baseSettings.UsedNetwork());

            var getTxCount =
                _outputRepository.GetTransactionsCount(btcAddress, atBlockHeight);
            var getBtcAmount = _outputRepository.GetBtcAmountSummary(btcAddress, atBlockHeight, colored);
            var getbtcReceived = _outputRepository.GetBtcReceivedSummary(btcAddress, atBlockHeight, colored);

            Task<IDictionary<string,long>> assetsReceiveds;
            Task<IDictionary<string, long>> assetsAmounts;


            if (colored)
            {
                assetsReceiveds = _outputRepository.GetAssetsReceived(btcAddress, atBlockHeight);
                assetsAmounts = _outputRepository.GetAssetsAmount(btcAddress, atBlockHeight);
            }
            else
            {

                IDictionary<string, long> emptyResuly = new Dictionary<string, long>();
                assetsReceiveds = Task.FromResult(emptyResuly);
                assetsAmounts = Task.FromResult(emptyResuly);
            }

            await Task.WhenAll(getTxCount, getBtcAmount, getbtcReceived, assetsReceiveds);

            return AddressSummaryViewModel.Create(getTxCount.Result, getBtcAmount.Result, getbtcReceived.Result, assetsReceiveds.Result, assetsAmounts.Result);
        }
    }
}
