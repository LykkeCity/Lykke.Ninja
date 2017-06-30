using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Ninja.Block;
using Core.Settings;
using Core.Transaction;
using Microsoft.AspNetCore.Mvc;
using Services.Ninja;
using Web.Models;

namespace Web.Controllers
{
    [Route("balances")]
    public class AddressTransactionsController:Controller
    {
        private readonly ITransactionOutputRepository _outputRepository;
        private readonly BaseSettings _baseSettings;
        private readonly INinjaBlockService _ninjaBlockService;


        public AddressTransactionsController(ITransactionOutputRepository outputRepository, 
            BaseSettings baseSettings, 
            INinjaBlockService ninjaBlockService)
        {
            _outputRepository = outputRepository;
            _baseSettings = baseSettings;
            _ninjaBlockService = ninjaBlockService;
        }

        [HttpGet("{address}")]
        public async Task<AddressTransactionsViewModel> Get(string address, [FromQuery]bool colored = false, [FromQuery]bool unspentonly = false, [FromQuery(Name = "from")]string maxBlockDescriptor = null, [FromQuery(Name = "to")]string minBlockDescriptor = null)
        {
            if (string.IsNullOrEmpty(maxBlockDescriptor) && string.IsNullOrEmpty(minBlockDescriptor))
            {
                return await GetTransactions(address, colored, unspentonly);
            }

            int maxBlockHeight;
            int minBlockHeight;
            if (int.TryParse(maxBlockDescriptor, out maxBlockHeight) && int.TryParse(minBlockDescriptor, out minBlockHeight))
            {
                return await GetTransactions(address, colored, unspentonly, minBlockHeight: minBlockHeight, maxBlockHeight:maxBlockHeight);
            }

            var getMaxBlockHeader = _ninjaBlockService.GetBlockHeader(maxBlockDescriptor);
            var getMinBlockHeader = _ninjaBlockService.GetBlockHeader(minBlockDescriptor);

            await Task.WhenAll(getMinBlockHeader, getMinBlockHeader);

            return await GetTransactions(address, colored, unspentonly, minBlockHeight: getMinBlockHeader.Result?.BlockHeight, maxBlockHeight: getMaxBlockHeader.Result?.BlockHeight);
        }

        private async Task<AddressTransactionsViewModel> GetTransactions(string address, bool colored, bool unspendOnly, int ? minBlockHeight = null, int? maxBlockHeight = null)
        {
            var bitcoinAddress = BitcoinAddressHelper.GetBitcoinAddress(address, _baseSettings.UsedNetwork());

            var getNinjaTop = _ninjaBlockService.GetTip();

            Task<IEnumerable<ITransactionOutput>> getSpended;
            if (!unspendOnly)
            {
                getSpended = _outputRepository.GetSpended(bitcoinAddress, minBlockHeight: minBlockHeight,
                    maxBlockHeight: maxBlockHeight);

            }
            else
            {
                getSpended = Task.FromResult(Enumerable.Empty<ITransactionOutput>());
            }

            var getReceived = _outputRepository.GetReceived(bitcoinAddress, unspendOnly, minBlockHeight: minBlockHeight, maxBlockHeight: maxBlockHeight);

            await Task.WhenAll(getNinjaTop, getSpended, getReceived);

            return AddressTransactionsViewModel.Create(getNinjaTop.Result, _baseSettings.UsedNetwork(), colored, getSpended.Result, getReceived.Result);
        }
    }
}
