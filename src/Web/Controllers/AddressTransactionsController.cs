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
        public async Task<AddressTransactionsViewModel> Get(string address, [FromQuery]string at = null)
        {
            if (string.IsNullOrEmpty(at))
            {
                return await GetTransactions(address);
            }

            int parsedHeight;

            if (int.TryParse(at.Trim(), out parsedHeight))
            {
                return await GetTransactions(address, parsedHeight);
            }

            var header = await _ninjaBlockService.GetBlockHeader(at);

            return await GetTransactions(address, header.BlockHeight);
        }

        private async Task<AddressTransactionsViewModel> GetTransactions(string address, int? blockHeight = null)
        {
            var bitcoinAddress = BitcoinAddressHelper.GetBitcoinAddress(address, _baseSettings.UsedNetwork());

            var getNinjaTop = _ninjaBlockService.GetTip();
            var getSpended = _outputRepository.GetSpended(bitcoinAddress, at: blockHeight);
            var getReceived = _outputRepository.GetReceived(bitcoinAddress, at: blockHeight);

            await Task.WhenAll(getNinjaTop, getSpended, getReceived);

            return AddressTransactionsViewModel.Create(getNinjaTop.Result, _baseSettings.UsedNetwork(), getSpended.Result, getReceived.Result);
        }
    }
}
