using System;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Core.AlertNotifications;
using Core.BlockStatus;
using Core.Ninja.Block;
using Core.ParseBlockCommand;
using Core.Queue;
using Core.Transaction;
using Lykke.JobTriggers.Triggers.Attributes;
using NBitcoin;

namespace Jobs.BlockTasks
{
    public class BlockTasksConsumerFunctions
    {
        private readonly INinjaBlockService _ninjaBlockService;
        private readonly IBlockStatusesRepository _blockStatusesRepository;
        private readonly ITransactionService _transactionService;

        private readonly ILog _log;
        private readonly ISlackNotificationsProducer _slack;
        private readonly IConsole _console;

        public BlockTasksConsumerFunctions(INinjaBlockService ninjaBlockService, 
            IBlockStatusesRepository blockStatusesRepository, 
            ILog log, 
            ISlackNotificationsProducer slack,
            IConsole console, 
            ITransactionService transactionService)
        {
            _ninjaBlockService = ninjaBlockService;
            _blockStatusesRepository = blockStatusesRepository;
            _log = log;
            _slack = slack;
            _console = console;
            _transactionService = transactionService;
        }

        [QueueTrigger(QueueNames.ParseBlockTasks, notify: true)]
        public async Task ParseBlock(ParseBlockCommandContext context)
        {
            try
            {
                _console.WriteLine($"{nameof(ParseBlock)} Block Height:{context.BlockHeight} Started");

                var getBlock = _ninjaBlockService.GetBlock(uint256.Parse(context.BlockId));
                var setStartedStatus = _blockStatusesRepository.SetGrabbedStatus(context.BlockId, InputOutputsGrabbedStatus.Started);

                await Task.WhenAll(getBlock, setStartedStatus);


                _console.WriteLine($"{nameof(ParseBlock)} Block Height:{context.BlockHeight} Insert data Started");

                await _transactionService.Insert(getBlock.Result);

                await _blockStatusesRepository.SetGrabbedStatus(context.BlockId, InputOutputsGrabbedStatus.Done);
                
                _console.WriteLine($"{nameof(ParseBlock)} Block Height:{context.BlockHeight} Done");
            }
            catch (Exception e)
            {
                await _log.WriteErrorAsync(nameof(BlockTasksConsumerFunctions), nameof(ParseBlock), context.ToJson(), e);
                await _slack.SendNotification(nameof(BlockTasksConsumerFunctions), nameof(ParseBlock), $"Error on {context.ToJson()}. Admin attention required");
                throw;
            }
        }
    }
}
