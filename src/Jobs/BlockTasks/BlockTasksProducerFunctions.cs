using System;
using System.Linq;
using System.Threading.Tasks;
using Common.Log;
using Core.AlertNotifications;
using Core.BlockStatus;
using Core.Ninja.Block;
using Core.ParseBlockCommand;
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

        public BlockTasksProducerFunctions(INinjaBlockService ninjaBlockService, 
            IParseBlockCommandsService commandProducer, 
            IBlockStatusesRepository blockStatusesRepository,
            ILog log, 
            ISlackNotificationsProducer slack, 
            IConsole console)
        {
            _ninjaBlockService = ninjaBlockService;
            _commandProducer = commandProducer;
            _blockStatusesRepository = blockStatusesRepository;
            _log = log;
            _slack = slack;
            _console = console;
        }

        [TimerTrigger("00:00:30")]
        public async Task ScanNewBlocks()
        {
            try
            {
                _console.WriteLine($"{nameof(ScanNewBlocks)} started");

                var getLastParsedBlock = _blockStatusesRepository.GetLastQueuedBlock();
                var getLastBlockInNija = _ninjaBlockService.GetTip();

                await Task.WhenAll(getLastParsedBlock, getLastBlockInNija);

                //var lastParsedBlock = getLastParsedBlock.Result?.BlockHeight ?? -1;
                var lastParsedBlock = getLastParsedBlock.Result?.BlockHeight ?? 472000;
                for (var blockHeight = lastParsedBlock + 1; blockHeight <= getLastBlockInNija.Result.BlockHeight; blockHeight++)
                {
                    await _commandProducer.ProduceParseBlockCommand(blockHeight);
                }
            }
            catch (Exception e)
            {

                await _log.WriteErrorAsync(nameof(BlockTasksProducerFunctions), nameof(ScanNewBlocks), null, e);
                await _slack.SendNotification(nameof(BlockTasksConsumerFunctions), nameof(ScanNewBlocks), e.Message);
                throw;
            }
        }
    }
}
