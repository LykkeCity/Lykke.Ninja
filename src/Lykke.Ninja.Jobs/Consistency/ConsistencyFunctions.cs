using System;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Lykke.Ninja.Core.Block;
using Lykke.Ninja.Core.Transaction;
using Lykke.JobTriggers.Triggers.Attributes;
using Lykke.Ninja.Core.AlertNotifications;
using Lykke.Ninja.Core.Ninja.Block;
using Lykke.Ninja.Core.Ninja.Transaction;
using Lykke.Ninja.Core.ParseBlockCommand;
using Lykke.Ninja.Core.Queue;
using NBitcoin;

namespace Lykke.Ninja.Jobs.Input
{
    public class ConsistencyFunctions
    {
        private readonly ITransactionInputRepository _inputRepository;
        private readonly IBlockService _blockService;
        private readonly ILog _log;
        private readonly IConsole _console;
        private readonly INinjaTransactionService _ninjaTransactionService;
        private readonly INinjaBlockService _ninjaBlockService;
        private readonly IProcessParseBlockCommandFacade _parseBlockCommandFacade;
        private readonly ISlackNotificationsProducer _slack;
        private readonly IScanNotFoundsCommandProducer _scanNotFoundsCommandProducer;

        public ConsistencyFunctions(ITransactionInputRepository inputRepository,
            IBlockService blockService, 
            ILog log, 
            IConsole console, 
            INinjaTransactionService ninjaTransactionService,
            INinjaBlockService ninjaBlockService,
            IProcessParseBlockCommandFacade parseBlockCommandFacade,
            ISlackNotificationsProducer slack, 
            IScanNotFoundsCommandProducer scanNotFoundsCommandProducer)
        {
            _inputRepository = inputRepository;
            _blockService = blockService;
            _log = log;
            _console = console;
            _ninjaTransactionService = ninjaTransactionService;
            _ninjaBlockService = ninjaBlockService;
            _parseBlockCommandFacade = parseBlockCommandFacade;
            _slack = slack;
            _scanNotFoundsCommandProducer = scanNotFoundsCommandProducer;
        }



        [TimerTrigger("01:00:00")]
        public async Task SetWaitingToSpend()
        {
            _console.WriteLine($"{nameof(ConsistencyFunctions)}.{nameof(SetWaitingToSpend)} started");

            var inputs = await _inputRepository.Get(SpendProcessedStatus.Waiting, itemsToTake: 5000);
            if (inputs.Any())
            {
                await _blockService.ProcessInputsToSpend(inputs);
            }
        }

        [TimerTrigger("00:01:00")]
        public async Task SetNotFoundSpendable()
        {
            _console.WriteLine($"{nameof(ConsistencyFunctions)}.{nameof(SetNotFoundSpendable)} started");

            var inputs = await _inputRepository.Get(SpendProcessedStatus.NotFound, itemsToTake: 5000);
            if (inputs.Any())
            {
                await _log.WriteWarningAsync(nameof(ConsistencyFunctions), nameof(SetNotFoundSpendable), inputs.Take(5).ToJson(),
                    "Processing not found inputs");

                var operationResult = await _blockService.ProcessInputsToSpend(inputs);

                if (operationResult.NotFound.Any())
                {
                    await _scanNotFoundsCommandProducer.CreateScanNotFoundsCommand();
                }
            }
        }

        [QueueTrigger(QueueNames.ScanNotFounds, notify: true, maxPollingIntervalMs: 60 * 1000)]
        public async Task ScanForNotFound(ScanNotFoundsContext context)
        {
            _console.WriteLine($"{nameof(ConsistencyFunctions)}.{nameof(ScanForNotFound)} started");

            var inputs = await _inputRepository.Get(SpendProcessedStatus.NotFound, itemsToTake: int.MaxValue);
            if (inputs.Any())
            {
                var txIds = inputs.Select(p => p.TxIn.TransactionId).Distinct().ToList();

                var txs = (await _ninjaTransactionService.Get(txIds.Select(uint256.Parse))).ToList();

                var heights = txs.Select(p => p.Block.Height).Distinct().OrderBy(p => p).ToList();

                foreach (var height in heights)
                {
                    try
                    {
                        var header = await _ninjaBlockService.GetBlockHeader(height);

                        await _slack.SendNotification(nameof(ScanForNotFound), $"Parse Block {header.ToJson()}");
                        await _parseBlockCommandFacade.ProcessCommand(
                            new ParseBlockCommandContext { BlockHeight = header.BlockHeight, BlockId = header.BlockId.ToString() });
                    }
                    catch (Exception e)
                    {
                        await _log.WriteErrorAsync(nameof(ConsistencyFunctions), nameof(ScanForNotFound), height.ToString(), e);
                    }
                }

                await SetNotFoundSpendable();
            }
        }
    }
}
