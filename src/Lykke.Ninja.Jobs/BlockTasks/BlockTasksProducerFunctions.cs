using System;
using System.Linq;
using System.Threading.Tasks;
using Common.Log;
using Core.AlertNotifications;
using Core.BlockStatus;
using Core.Ninja.Block;
using Core.ParseBlockCommand;
using Core.Queue;
using Core.Settings;
using Lykke.JobTriggers.Triggers.Attributes;

namespace Jobs.BlockTasks
{
    public class BlockTasksProducerFunctions
    {
        private readonly INinjaBlockService _ninjaBlockService;
        private readonly IParseBlockCommandsService _commandProducer;
        private readonly IBlockStatusesRepository _blockStatusesRepository;
        private readonly ILog _log;
        private readonly ISlackNotificationsProducer _slack;
        private readonly IConsole _console;
        private readonly BaseSettings _baseSettings;

        public BlockTasksProducerFunctions(INinjaBlockService ninjaBlockService, 
            IParseBlockCommandsService commandProducer, 
            IBlockStatusesRepository blockStatusesRepository,
            ILog log, 
            ISlackNotificationsProducer slack, 
            IConsole console, BaseSettings baseSettings)
        {
            _ninjaBlockService = ninjaBlockService;
            _commandProducer = commandProducer;
            _blockStatusesRepository = blockStatusesRepository;
            _log = log;
            _slack = slack;
            _console = console;
            _baseSettings = baseSettings;
        }


        [TimerTrigger("00:00:10")]
        public async Task ScanNewBlocks()
        {
            try
            {
                _console.WriteLine($"{nameof(ScanNewBlocks)} started");

                var getLastParsedBlock = _blockStatusesRepository.GetLastQueuedBlock();
                var getLastBlockInNija = _ninjaBlockService.GetTip();

                await Task.WhenAll(getLastParsedBlock, getLastBlockInNija);

                var lastParsedBlock = getLastParsedBlock.Result?.Height ?? -1;

                var queuedCommandCount = await _commandProducer.GetQueuedCommandCount();
                for (var blockHeight = lastParsedBlock + 1; blockHeight <= getLastBlockInNija.Result.BlockHeight; blockHeight++)
                {
                    if (queuedCommandCount >= _baseSettings.MaxParseBlockQueuedCommandCount)
                    {
                        break;
                    }

                    await _commandProducer.ProduceParseBlockCommand(blockHeight);

                    queuedCommandCount++;
                }
            }
            catch (Exception e)
            {
                await _log.WriteErrorAsync(nameof(BlockTasksProducerFunctions), nameof(ScanNewBlocks), null, e);
                await _slack.SendNotification(nameof(BlockTasksConsumerFunctions), nameof(ScanNewBlocks), e.Message);
                throw;
            }
        }

        [TimerTrigger("00:00:30")]
        public async Task PutFailedToQueueAgain()
        {
            try
            {
                _console.WriteLine($"{nameof(PutFailedToQueueAgain)} started");
                var failedBlocks = await _blockStatusesRepository.GetAll(BlockProcessingStatus.Fail, 50);

                foreach (var blockStatus in failedBlocks.Where(p => p.StatusChangedAt > DateTime.UtcNow.AddMinutes(-10)))
                {
                    await _commandProducer.ProduceParseBlockCommand(blockStatus.Height);
                }
            }
            catch (Exception e)
            {
                await _log.WriteErrorAsync(nameof(BlockTasksProducerFunctions), nameof(PutFailedToQueueAgain), null, e);
                await _slack.SendNotification(nameof(BlockTasksConsumerFunctions), nameof(PutFailedToQueueAgain), e.Message);
                throw;
            }
        }
    }
}
