using System.Threading.Tasks;
using Core.Ninja.Block;
using Core.Settings;
using Core.Transaction;
using Microsoft.AspNetCore.Mvc;
using NBitcoin;
using Services.Ninja;
using Services.Settings;
using Web.Models;

namespace Web.Controllers
{
    [Route("balances")]
    public class BalanceSummaryController : Controller
    {
        private readonly INinjaBlockService _ninjaBlockService;
        private readonly ITransactionOutputRepository _outputRepository;
        private readonly BaseSettings _baseSettings;

        public BalanceSummaryController(INinjaBlockService ninjaBlockService, 
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
            var getBtcAmount = _outputRepository.GetBtcAmount(btcAddress, atBlockHeight, colored);
            var getbtcReceived = _outputRepository.GetBtcReceived(btcAddress, atBlockHeight, colored);
            var assetsReceiveds = _outputRepository.GetAssetsReceived(btcAddress, atBlockHeight);
            var assetsAmounts = _outputRepository.GetAssetsAmount(btcAddress, atBlockHeight);

            await Task.WhenAll(getTxCount, getBtcAmount, getbtcReceived, assetsReceiveds);

            return AddressSummaryViewModel.Create(getTxCount.Result, getBtcAmount.Result, getbtcReceived.Result, assetsReceiveds.Result, assetsAmounts.Result);
        }
    }
}
