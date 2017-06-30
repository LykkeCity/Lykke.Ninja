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
            var getQueuedCount = _blockCommandProducer.GetQueuedCommandCount();
            var getLastQueuedBlock = _blockStatusesRepository.GetLastQueuedBlock();
            var getFailedBlocks = _blockStatusesRepository.GetAll(BlockProcessingStatus.Fail);
            var getQueuedBlocks = _blockStatusesRepository.GetAll(BlockProcessingStatus.Queued);
            var getNotFoundInputs = _inputRepository.Get(SpendProcessedStatus.NotFound);
            var getWaitingInputs = _inputRepository.Get(SpendProcessedStatus.Waiting);

            await Task.WhenAll(getQueuedCount, getLastQueuedBlock, getFailedBlocks, getQueuedBlocks,  getNotFoundInputs , getWaitingInputs);

            return ConsistencyCheckViewModel.Create(getQueuedCount.Result, 
                getLastQueuedBlock.Result, 
                getWaitingInputs.Result, 
                getNotFoundInputs.Result,
                getFailedBlocks.Result,
                getQueuedBlocks.Result);
        }
    }
}
