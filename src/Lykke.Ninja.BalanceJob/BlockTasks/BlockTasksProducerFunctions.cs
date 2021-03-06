﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Lykke.JobTriggers.Triggers.Attributes;
using Lykke.Ninja.Core.AlertNotifications;
using Lykke.Ninja.Core.BlockStatus;
using Lykke.Ninja.Core.Ninja.Block;
using Lykke.Ninja.Core.ParseBlockCommand;
using Lykke.Ninja.Core.Settings;

namespace Lykke.Ninja.BalanceJob.BlockTasks
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
                await _slack.SendError(nameof(ScanNewBlocks), e.Message);
                throw;
            }
        }

        [TimerTrigger("00:01:00")]
        public async Task PutFailedToQueueAgain()
        {
            try
			{
				var queuedCommandCount = await _commandProducer.GetQueuedCommandCount();
				if (queuedCommandCount < _baseSettings.MaxParseBlockQueuedCommandCount)
				{
					var failedBlocks = await _blockStatusesRepository.GetList(BlockProcessingStatus.Fail, 10);

					foreach (var blockStatus in failedBlocks.Where(p => p.StatusChangedAt < DateTime.UtcNow.AddMinutes(-60)))
					{
						await _commandProducer.ProduceParseBlockCommand(blockStatus.Height);

						await _log.WriteInfoAsync(nameof(BlockTasksProducerFunctions), nameof(PutFailedToQueueAgain), new { height = blockStatus.Height, id = blockStatus.BlockId }.ToJson(), "Put to queue again");
					}
				}
            }
            catch (Exception e)
            {
                await _log.WriteErrorAsync(nameof(BlockTasksProducerFunctions), nameof(PutFailedToQueueAgain), null, e);
                await _slack.SendError(nameof(PutFailedToQueueAgain), e.Message);
                throw;
            }
        }

	    [TimerTrigger("00:10:00")]
	    public async Task PutStackedStartedToQueueAgain()
	    {
		    try
		    {
			    var queuedCommandCount = await _commandProducer.GetQueuedCommandCount();
			    if (queuedCommandCount <_baseSettings.MaxParseBlockQueuedCommandCount)
			    {
				    var blocks = await _blockStatusesRepository.GetList(BlockProcessingStatus.Started, 10);

				    foreach (var blockStatus in blocks.Where(p => p.StatusChangedAt < DateTime.UtcNow.AddMinutes(-60)))
				    {
						await _commandProducer.ProduceParseBlockCommand(blockStatus.Height);
						await _log.WriteInfoAsync(nameof(BlockTasksProducerFunctions), nameof(PutStackedStartedToQueueAgain), new { height = blockStatus.Height, id = blockStatus.BlockId }.ToJson(), "Put to queue again");
					}
			    }
		    }
		    catch (Exception e)
		    {
			    await _log.WriteErrorAsync(nameof(BlockTasksProducerFunctions), nameof(PutFailedToQueueAgain), null, e);
			    await _slack.SendError(nameof(PutFailedToQueueAgain), e.Message);
			    throw;
		    }
	    }

	    [TimerTrigger("00:10:00")]
	    public async Task PutStackedQueuedToQueueAgain()
	    {
		    try
		    {
			    var queuedCommandCount = await _commandProducer.GetQueuedCommandCount();
			    if (queuedCommandCount < _baseSettings.MaxParseBlockQueuedCommandCount)
			    {
				    var blocks = await _blockStatusesRepository.GetList(BlockProcessingStatus.Queued, 10);

				    foreach (var blockStatus in blocks.Where(p => p.StatusChangedAt < DateTime.UtcNow.AddMinutes(-60)))
					{
						await _commandProducer.ProduceParseBlockCommand(blockStatus.Height);

						await _log.WriteInfoAsync(nameof(BlockTasksProducerFunctions), nameof(PutStackedQueuedToQueueAgain), new { height = blockStatus.Height, id = blockStatus.BlockId }.ToJson(), "Put to queue again");
					}
				}
		    }
		    catch (Exception e)
		    {
			    await _log.WriteErrorAsync(nameof(BlockTasksProducerFunctions), nameof(PutFailedToQueueAgain), null, e);
			    await _slack.SendError(nameof(PutFailedToQueueAgain), e.Message);
			    throw;
		    }
	    }
	}
}
