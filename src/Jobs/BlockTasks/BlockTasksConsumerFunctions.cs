using System;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Core.AlertNotifications;
using Core.Block;
using Core.BlockStatus;
using Core.Ninja.Block;
using Core.Ninja.Transaction;
using Core.ParseBlockCommand;
using Core.Queue;
using Lykke.JobTriggers.Triggers.Attributes;
using NBitcoin;
using NBitcoin.OpenAsset;

namespace Jobs.BlockTasks
{
    public class BlockTasksConsumerFunctions
    {
        private readonly INinjaBlockService _ninjaBlockService;
        private readonly INinjaTransactionService _ninjaTransactionService;
        private readonly IBlockStatusesRepository _blockStatusesRepository;
        private readonly IBlockService _blockService;

        private readonly ILog _log;
        private readonly ISlackNotificationsProducer _slack;
        private readonly IConsole _console;

        public BlockTasksConsumerFunctions(INinjaBlockService ninjaBlockService, 
            IBlockStatusesRepository blockStatusesRepository, 
            ILog log, 
            ISlackNotificationsProducer slack,
            IConsole console, 
            IBlockService blockService, 
            INinjaTransactionService ninjaTransactionService)
        {
            _ninjaBlockService = ninjaBlockService;
            _blockStatusesRepository = blockStatusesRepository;
            _log = log;
            _slack = slack;
            _console = console;
            _blockService = blockService;
            _ninjaTransactionService = ninjaTransactionService;
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

                _console.WriteLine($"{nameof(ParseBlock)} Block Height:{context.BlockHeight} Get Transactions started");


                var coloredTransactions = await _ninjaTransactionService.Get(
                    getBlock.Result.Block.Transactions
                        .Where(p => p.HasValidColoredMarker())
                        .Select(p => p.GetHash()));

                _console.WriteLine($"{nameof(ParseBlock)} Block Height:{context.BlockHeight} Insert data Started");
                
                await _blockService.Parse(getBlock.Result, coloredTransactions);

                await _blockStatusesRepository.SetGrabbedStatus(context.BlockId, InputOutputsGrabbedStatus.Done);
                
                _console.WriteLine($"{nameof(ParseBlock)} Block Height:{context.BlockHeight} Done");
            }
            catch (Exception e)
            {
                await _log.WriteErrorAsync(nameof(BlockTasksConsumerFunctions), nameof(ParseBlock), context.ToJson(), e);
                await _slack.SendNotification(nameof(BlockTasksConsumerFunctions), nameof(ParseBlock), $"Error on {context.ToJson()}. Admin attention required");
                await _blockStatusesRepository.SetGrabbedStatus(context.BlockId, InputOutputsGrabbedStatus.Fail);

                throw;
            }
        }
    }
}
