using System.Threading.Tasks;
using Core.BlockStatus;
using Core.ParseBlockCommand;
using Core.Transaction;
using Microsoft.AspNetCore.Mvc;
using Web.Models;

namespace Web.Controllers
{
    [Route("health")]
    public class HealthController:Controller
    {
        private readonly IParseBlockCommandProducer _blockCommandProducer;
        private readonly IBlockStatusesRepository _blockStatusesRepository;
        private readonly ITransactionInputRepository _inputRepository;

        public HealthController(IParseBlockCommandProducer blockCommandProducer, 
            IBlockStatusesRepository blockStatusesRepository,
            ITransactionInputRepository inputRepository)
        {
            _blockCommandProducer = blockCommandProducer;
            _blockStatusesRepository = blockStatusesRepository;
            _inputRepository = inputRepository;
        }

        [HttpGet("consistency/check")]
        public async Task<ConsistencyCheckViewModel> ConsistencyCheck()
        {
            const int showLastItemsCount = 5;
            var getQueuedCount = _blockCommandProducer.GetQueuedCommandCount();
            var getLastQueuedBlock = _blockStatusesRepository.GetLastQueuedBlock();

            var getFailedBlocks = _blockStatusesRepository.GetAll(BlockProcessingStatus.Fail, itemsToTake: showLastItemsCount);
            var getFailedBlockCount = _blockStatusesRepository.Count(BlockProcessingStatus.Fail);

            var getQueuedBlocks = _blockStatusesRepository.GetAll(BlockProcessingStatus.Queued, itemsToTake: showLastItemsCount);
            var getQueuedBlockCount = _blockStatusesRepository.Count(BlockProcessingStatus.Queued);

            var getNotFoundInputs = _inputRepository.Get(SpendProcessedStatus.NotFound, itemsToTake: showLastItemsCount);
            var getNotFoundInputsCount = _inputRepository.Count(SpendProcessedStatus.NotFound);

            var getWaitingInputs = _inputRepository.Get(SpendProcessedStatus.Waiting, itemsToTake: showLastItemsCount);
            var getWaitingInputsCount = _inputRepository.Count(SpendProcessedStatus.NotFound);
            await Task.WhenAll(getQueuedCount, 
                getLastQueuedBlock, 
                getFailedBlocks, 
                getFailedBlockCount,
                getQueuedBlocks, 
                getQueuedBlockCount,
                getNotFoundInputs, 
                getWaitingInputs, 
                getNotFoundInputsCount,
                getWaitingInputsCount);

            return ConsistencyCheckViewModel.Create(getQueuedCount.Result, 
                getLastQueuedBlock.Result, 
                getWaitingInputs.Result, 
                getWaitingInputsCount.Result,
                getNotFoundInputs.Result,
                getNotFoundInputsCount.Result,
                getFailedBlocks.Result,
                getFailedBlockCount.Result,
                getQueuedBlocks.Result,
                getQueuedBlockCount.Result);
        }
    }
}
