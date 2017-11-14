using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Ninja.Core.BlockStatus;
using Lykke.Ninja.Core.Ninja.Block;
using Lykke.Ninja.Core.Settings;
using Lykke.Ninja.Core.Transaction;
using Lykke.Ninja.Core.UnconfirmedBalances.BalanceChanges;
using Microsoft.AspNetCore.Mvc;
using Lykke.Ninja.Services.Ninja;
using Lykke.Ninja.Web.Models;
using NBitcoin;

namespace Lykke.Ninja.Web.Controllers
{
    [Route("balances")]
    public class AddressTransactionsController:Controller
    {
        private readonly ITransactionOutputRepository _outputRepository;
        private readonly Network _network;
        private readonly INinjaBlockService _ninjaBlockService;
        private readonly IBlockStatusesRepository _blockStatusesRepository;
        private readonly IUnconfirmedBalanceChangesRepository _unconfirmedBalanceChangesRepository;
        private readonly int _itemsOnAddressTransactionPage;


        public AddressTransactionsController(ITransactionOutputRepository outputRepository,
            Network network, 
            INinjaBlockService ninjaBlockService,
            IBlockStatusesRepository blockStatusesRepository,
            IUnconfirmedBalanceChangesRepository unconfirmedBalanceChangesRepository,
            BaseSettings baseSettings)
        {
            _outputRepository = outputRepository;
            _network = network;
            _ninjaBlockService = ninjaBlockService;
            _blockStatusesRepository = blockStatusesRepository;
            _unconfirmedBalanceChangesRepository = unconfirmedBalanceChangesRepository;
            _itemsOnAddressTransactionPage = baseSettings.ItemsOnAddressTransactionPage;
        }

        [HttpGet("{address}")]
        public async Task<TransactionsViewModel> Get(string address, 
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

        private async Task<TransactionsViewModel> GetTransactions(string address, 
            bool colored, 
            bool unspendOnly, 
            string continuation,
            int? minBlockHeight = null, 
            int? maxBlockHeight = null)
        {
            var bitcoinAddress = BitcoinAddressHelper.GetBitcoinAddress(address, _network);

            var getNinjaTop = _ninjaBlockService.GetTip(withRetry:false);
            var itemsToSkip = ContiniationBinder.GetItemsToSkipFromContinuationToke(continuation);
            
            Task<IEnumerable<ITransactionOutput>> getSpended;
            if (!unspendOnly)
            {
                getSpended = _outputRepository.GetSpended(bitcoinAddress, 
                    minBlockHeight: minBlockHeight,
                    maxBlockHeight: maxBlockHeight,
                    itemsToSkip: itemsToSkip,
                    itemsToTake: _itemsOnAddressTransactionPage);
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
                itemsToTake: _itemsOnAddressTransactionPage);

            Task<IEnumerable<IBalanceChange>> getUnconfirmedSpended;
            Task<IEnumerable<IBalanceChange>> getUnconfirmedReceived;

            if (minBlockHeight == null)
            {
                getUnconfirmedSpended = _unconfirmedBalanceChangesRepository.GetSpended(bitcoinAddress.ToString());
                getUnconfirmedReceived = _unconfirmedBalanceChangesRepository.GetReceived(bitcoinAddress.ToString());
            }
            else
            {
                getUnconfirmedSpended = Task.FromResult(Enumerable.Empty<IBalanceChange>());
                getUnconfirmedReceived = Task.FromResult(Enumerable.Empty<IBalanceChange>());
            }

            await Task.WhenAll(getNinjaTop, 
                getSpended, 
                getReceived,
                getUnconfirmedSpended,
                getUnconfirmedReceived);

            string newContinuation = null;
            if (getSpended.Result.Count() == _itemsOnAddressTransactionPage ||
                getReceived.Result.Count() == _itemsOnAddressTransactionPage)
            {
                newContinuation =
                    ContiniationBinder.GetContinuationToken(itemsToSkip ,_itemsOnAddressTransactionPage);
            }

            return TransactionsViewModel.Create(getNinjaTop.Result, 
                _network, 
                colored, 
                getSpended.Result, 
                getReceived.Result,
                newContinuation,
                getUnconfirmedSpended.Result,
                getUnconfirmedReceived.Result);
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
