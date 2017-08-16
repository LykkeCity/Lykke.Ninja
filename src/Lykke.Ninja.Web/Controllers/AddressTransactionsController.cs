using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
    public class AddressTransactionsController:Controller
    {
        private readonly ITransactionOutputRepository _outputRepository;
        private readonly BaseSettings _baseSettings;
        private readonly INinjaBlockService _ninjaBlockService;
        private readonly IBlockStatusesRepository _blockStatusesRepository;
        

        public AddressTransactionsController(ITransactionOutputRepository outputRepository, 
            BaseSettings baseSettings, 
            INinjaBlockService ninjaBlockService,
            IBlockStatusesRepository blockStatusesRepository)
        {
            _outputRepository = outputRepository;
            _baseSettings = baseSettings;
            _ninjaBlockService = ninjaBlockService;
            _blockStatusesRepository = blockStatusesRepository;
        }

        [HttpGet("{address}")]
        public async Task<AddressTransactionsViewModel> Get(string address, 
            [FromQuery]bool colored = false, 
            [FromQuery]bool unspentonly = false, 
            [FromQuery(Name = "from")]string maxBlockDescriptor = null, 
            [FromQuery(Name = "until")]string minBlockDescriptor = null,
            [FromQuery]string continuation = null)

        {
            var getMaxBlockHeight = GetMaxBlockHeight(maxBlockDescriptor);
            var getMinBlockHeight = GetMinBlockHeight(minBlockDescriptor);

            await Task.WhenAll(getMaxBlockHeight, getMaxBlockHeight);

            return await GetTransactions(address, 
                colored, 
                unspentonly,
                continuation,
                minBlockHeight: getMinBlockHeight.Result, 
                maxBlockHeight: getMaxBlockHeight.Result);
        }

        private async Task<AddressTransactionsViewModel> GetTransactions(string address, 
            bool colored, 
            bool unspendOnly, 
            string continuation,
            int? minBlockHeight = null, 
            int? maxBlockHeight = null)
        {
            var bitcoinAddress = BitcoinAddressHelper.GetBitcoinAddress(address, _baseSettings.UsedNetwork());

            var getNinjaTop = _ninjaBlockService.GetTip(withRetry:false);
            var itemsToSkip = ContiniationBinder.GetItemsToSkipFromContinuationToke(continuation);
            
            Task<IEnumerable<ITransactionOutput>> getSpended;
            if (!unspendOnly)
            {
                getSpended = _outputRepository.GetSpended(bitcoinAddress, 
                    minBlockHeight: minBlockHeight,
                    maxBlockHeight: maxBlockHeight,
                    itemsToSkip: itemsToSkip,
                    itemsToTake: _baseSettings.ItemsOnAddressTransactionPage);
            }
            else
            {
                getSpended = Task.FromResult(Enumerable.Empty<ITransactionOutput>());
            }
            
            var getReceived = _outputRepository.GetReceived(bitcoinAddress, 
                unspendOnly, 
                minBlockHeight: minBlockHeight, 
                maxBlockHeight: maxBlockHeight,
                itemsToSkip: itemsToSkip,
                itemsToTake: _baseSettings.ItemsOnAddressTransactionPage);
            

            await Task.WhenAll(getNinjaTop, 
                getSpended, 
                getReceived);

            string newContinuation = null;
            if (getSpended.Result.Count() == _baseSettings.ItemsOnAddressTransactionPage ||
                getReceived.Result.Count() == _baseSettings.ItemsOnAddressTransactionPage)
            {
                newContinuation =
                    ContiniationBinder.GetContinuationToken(itemsToSkip ,_baseSettings.ItemsOnAddressTransactionPage);
            }

            return AddressTransactionsViewModel.Create(getNinjaTop.Result, 
                _baseSettings.UsedNetwork(), 
                colored, 
                newContinuation,
                getSpended.Result, 
                getReceived.Result);
        }


        private Task<int?> GetMinBlockHeight(string descriptor)
        {
            return GetBlockHeight(descriptor);
        }

        private async Task<int?> GetMaxBlockHeight(string descriptor)
        {
            if (string.IsNullOrEmpty(descriptor))
            {
                return await _blockStatusesRepository.GetLastBlockHeight(BlockProcessingStatus.Done);
            }

            return await GetBlockHeight(descriptor);
        }

        private async Task<int?> GetBlockHeight(string descriptor)
        {
            if (string.IsNullOrEmpty(descriptor))
            {
                return null;
            }

            if (NinjaBlockHelper.IsTopBlock(descriptor))
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
