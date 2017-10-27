using System.Threading.Tasks;
using Lykke.Ninja.Core.BlockStatus;
using Lykke.Ninja.Core.Ninja.Block;
using Lykke.Ninja.Core.ParseBlockCommand;
using Lykke.Ninja.Core.Transaction;
using Lykke.Ninja.Core.UnconfirmedBalances.BalanceChanges;
using Lykke.Ninja.Core.UnconfirmedBalances.Statuses;
using Microsoft.AspNetCore.Mvc;
using Lykke.Ninja.Web.Models;

namespace Lykke.Ninja.Web.Controllers
{
    [Route("health")]
    public class HealthController:Controller
    {
        private readonly IParseBlockCommandProducer _blockCommandProducer;
        private readonly IBlockStatusesRepository _blockStatusesRepository;
        private readonly ITransactionInputRepository _inputRepository;
        private readonly INinjaBlockService _ninjaBlockService;
        private readonly IUnconfirmedStatusesRepository _unconfirmedStatusesRepository;
	    private readonly IUnconfirmedBalanceChangesRepository _unconfirmedBalanceChangesRepository;

		public HealthController(IParseBlockCommandProducer blockCommandProducer, 
            IBlockStatusesRepository blockStatusesRepository,
            ITransactionInputRepository inputRepository,
            INinjaBlockService ninjaBlockService, IUnconfirmedStatusesRepository unconfirmedStatusesRepository, 
			IUnconfirmedBalanceChangesRepository unconfirmedBalanceChangesRepository)
        {
            _blockCommandProducer = blockCommandProducer;
            _blockStatusesRepository = blockStatusesRepository;
            _inputRepository = inputRepository;
            _ninjaBlockService = ninjaBlockService;
            _unconfirmedStatusesRepository = unconfirmedStatusesRepository;
	        _unconfirmedBalanceChangesRepository = unconfirmedBalanceChangesRepository;
        }

        [HttpGet("check")]
        public async Task<HealthCheckViewModel> Check()
        {
            const int showLastItemsCount = 5;

            var getNinjaTopHeader = _ninjaBlockService.GetTip(false);
            var getQueuedCount = _blockCommandProducer.GetQueuedCommandCount();
            var getLastQueuedBlock = _blockStatusesRepository.GetLastQueuedBlock();

            var getFailedBlocks = _blockStatusesRepository.GetList(BlockProcessingStatus.Fail, itemsToTake: showLastItemsCount);
            var getFailedBlockCount = _blockStatusesRepository.Count(BlockProcessingStatus.Fail);

            var getQueuedBlocks = _blockStatusesRepository.GetList(BlockProcessingStatus.Queued, itemsToTake: showLastItemsCount);
            var getQueuedBlockCount = _blockStatusesRepository.Count(BlockProcessingStatus.Queued);

            var getNotFoundInputs = _inputRepository.Get(SpendProcessedStatus.NotFound, itemsToTake: showLastItemsCount);
            var getNotFoundInputsCount = _inputRepository.Count(SpendProcessedStatus.NotFound);

            var getWaitingInputs = _inputRepository.Get(SpendProcessedStatus.Waiting, itemsToTake: showLastItemsCount);
            var getWaitingInputsCount = _inputRepository.Count(SpendProcessedStatus.NotFound);

            var getProccessingBlocks =
                _blockStatusesRepository.GetList(BlockProcessingStatus.Started, itemsToTake: showLastItemsCount);
            var getLastSuccesfullyProcessedBlockHeight =
                _blockStatusesRepository.GetLastBlockHeight(BlockProcessingStatus.Done);

            var getFailedUnconfirmedTxCount = _unconfirmedStatusesRepository.GetNotRemovedTxCount(InsertProcessStatus.Failed);
            var getAllStatusesTxCount = _unconfirmedStatusesRepository.GetAllTxCount();
	        var getAllBalanceChangesTxCount = _unconfirmedBalanceChangesRepository.GetNotRemovedTxCount();
			var getWaitingUnconfirmedTxCount = _unconfirmedStatusesRepository.GetNotRemovedTxCount(InsertProcessStatus.Waiting);
			
            await Task.WhenAll(getQueuedCount, 
                getLastQueuedBlock, 
                getFailedBlocks, 
                getFailedBlockCount,
                getQueuedBlocks, 
                getQueuedBlockCount,
                getNotFoundInputs, 
                getWaitingInputs, 
                getNotFoundInputsCount,
                getWaitingInputsCount,
                getProccessingBlocks,
                getNinjaTopHeader, 
                getLastSuccesfullyProcessedBlockHeight,
                getFailedUnconfirmedTxCount, 
                getAllStatusesTxCount,
                getWaitingUnconfirmedTxCount,
	            getAllBalanceChangesTxCount);

            return HealthCheckViewModel.Create(getQueuedCount.Result, 
                getLastQueuedBlock.Result, 
                getWaitingInputs.Result, 
                getWaitingInputsCount.Result,
                getNotFoundInputs.Result,
                getNotFoundInputsCount.Result,
                getFailedBlocks.Result,
                getFailedBlockCount.Result,
                getQueuedBlocks.Result,
                getQueuedBlockCount.Result,
                getProccessingBlocks.Result,
                getNinjaTopHeader.Result,
                getLastSuccesfullyProcessedBlockHeight.Result,
                getFailedUnconfirmedTxCount.Result,
                getAllStatusesTxCount.Result,
                getWaitingUnconfirmedTxCount.Result,
				getAllBalanceChangesTxCount.Result);
        }
    }
}
