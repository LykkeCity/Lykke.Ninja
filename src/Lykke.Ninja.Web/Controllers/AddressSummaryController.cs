using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Lykke.Ninja.Core.Ninja.Block;
using Lykke.Ninja.Core.Settings;
using Lykke.Ninja.Core.Transaction;
using Lykke.Ninja.Core.UnconfirmedBalances.BalanceChanges;
using Microsoft.AspNetCore.Mvc;
using Lykke.Ninja.Services.Ninja;
using Lykke.Ninja.Web.Models;

namespace Lykke.Ninja.Web.Controllers
{
    [Route("balances")]
    public class AddressSummaryController : Controller
    {
        private readonly ICachedNinjaBlockService _ninjaBlockService;
        private readonly ITransactionOutputRepository _outputRepository;
        private readonly BaseSettings _baseSettings;
        private readonly IUnconfirmedBalanceChangesRepository _unconfirmedBalanceChangesRepository;

        public AddressSummaryController(ICachedNinjaBlockService ninjaBlockService, 
            ITransactionOutputRepository outputRepository, 
            BaseSettings baseSettings, IUnconfirmedBalanceChangesRepository unconfirmedBalanceChangesRepository)
        {
            _ninjaBlockService = ninjaBlockService;
            _outputRepository = outputRepository;
            _baseSettings = baseSettings;
            _unconfirmedBalanceChangesRepository = unconfirmedBalanceChangesRepository;
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
            var getSpendedTxCount = _outputRepository.GetSpendTransactionsCount(btcAddress, getAtBlockHeight.Result);

            var getBtcAmount = _outputRepository.GetBtcAmountSummary(btcAddress, 
                getAtBlockHeight.Result, 
                colored);

            var getbtcReceived = _outputRepository.GetBtcReceivedSummary(btcAddress, 
                getAtBlockHeight.Result, 
                colored);

            Task<IReadOnlyDictionary<string, long>> getAssetsReceived;
            Task<IReadOnlyDictionary<string, long>> getAssetsAmount;
            if (colored)
            {
                getAssetsReceived = _outputRepository.GetAssetsReceived(btcAddress,
                    getAtBlockHeight.Result);

                getAssetsAmount = _outputRepository.GetAssetsAmount(btcAddress,
                    getAtBlockHeight.Result);
            }
            else
            {
                IReadOnlyDictionary<string, long> emptyResult = new ReadOnlyDictionary<string, long>(new Dictionary<string, long>());
                getAssetsReceived = Task.FromResult(emptyResult);
                getAssetsAmount = Task.FromResult(emptyResult);
            }

            Task<long> getUnconfirmedTxCount;
            Task<long> getUnconfirmedBtcAmount;
            Task<long> getUnconfirmedBtcReceived;

            Task<IReadOnlyDictionary<string, long>> getUnconfirmedAssetsReceiveds;
            Task<IReadOnlyDictionary<string, long>> getUnconfirmedAssetsAmounts;

            if (getAtBlockHeight.Result == null)
            {
                getUnconfirmedTxCount = _unconfirmedBalanceChangesRepository.GetTransactionsCount(btcAddress.ToString());
                getUnconfirmedBtcAmount = _unconfirmedBalanceChangesRepository.GetBtcAmountSummary(btcAddress.ToString(), colored);
                getUnconfirmedBtcReceived = _unconfirmedBalanceChangesRepository.GetBtcReceivedSummary(btcAddress.ToString(), colored);

                if (colored)
                {
                    getUnconfirmedAssetsReceiveds = _unconfirmedBalanceChangesRepository.GetAssetsReceived(btcAddress.ToString());
                    getUnconfirmedAssetsAmounts = _unconfirmedBalanceChangesRepository.GetAssetsAmount(btcAddress.ToString());
                }
                else
                {
                    IReadOnlyDictionary<string, long> emptyResult = new ReadOnlyDictionary<string, long>(new Dictionary<string, long>());
                    getUnconfirmedAssetsReceiveds = Task.FromResult(emptyResult);
                    getUnconfirmedAssetsAmounts = Task.FromResult(emptyResult);
                }
            }
            else
            {
                getUnconfirmedTxCount = Task.FromResult(0L);
                getUnconfirmedBtcAmount = Task.FromResult(0L);
                getUnconfirmedBtcReceived = Task.FromResult(0L);


                IReadOnlyDictionary<string, long> emptyResult = new Dictionary<string, long>();
                getUnconfirmedAssetsReceiveds = Task.FromResult(emptyResult);
                getUnconfirmedAssetsAmounts = Task.FromResult(emptyResult);
            }


            await Task.WhenAll(getTxCount, 
                getSpendedTxCount,  
                getBtcAmount, 
                getbtcReceived, 
                getAssetsReceived, 
                getAssetsAmount,
                getUnconfirmedTxCount,
                getUnconfirmedBtcAmount,
                getUnconfirmedBtcReceived,
                getUnconfirmedAssetsReceiveds,
                getUnconfirmedAssetsAmounts);

            return AddressSummaryViewModel.Create(getTxCount.Result, 
                getSpendedTxCount.Result,
                getBtcAmount.Result, 
                getbtcReceived.Result,
                getAssetsReceived.Result,
                getAssetsAmount.Result,
                getUnconfirmedTxCount.Result,
                getUnconfirmedBtcAmount.Result,
                getUnconfirmedBtcReceived.Result,
                getUnconfirmedAssetsReceiveds.Result,
                getUnconfirmedAssetsAmounts.Result);
        }

        private async Task<int?> GetBlockHeight(string descriptor)
        {
            if (string.IsNullOrEmpty(descriptor) || NinjaBlockHelper.IsTopBlock(descriptor))
            {
                return null;
            }

            int parsedValue;

            if (int.TryParse(descriptor, out parsedValue))
            {
                return parsedValue;
            }


            var blockHeader = await _ninjaBlockService.GetBlockHeader(descriptor);

            return blockHeader?.BlockHeight;
        }
    }
}
