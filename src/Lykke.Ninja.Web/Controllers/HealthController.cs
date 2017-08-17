using System.Threading.Tasks;
using Lykke.Ninja.Core.BlockStatus;
using Lykke.Ninja.Core.ParseBlockCommand;
using Lykke.Ninja.Core.Transaction;
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

        public HealthController(IParseBlockCommandProducer blockCommandProducer, 
            IBlockStatusesRepository blockStatusesRepository,
            ITransactionInputRepository inputRepository)
        {
            _blockCommandProducer = blockCommandProducer;
            _blockStatusesRepository = blockStatusesRepository;
            _inputRepository = inputRepository;
        }

        [HttpGet("check")]
        public async Task<HealthCheckViewModel> Check()
        {
            const int showLastItemsCount = 5;

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
                getProccessingBlocks);

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
                getProccessingBlocks.Result);
        }
    }
}
