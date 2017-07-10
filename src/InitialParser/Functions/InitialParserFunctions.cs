using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Log;
using Core.AlertNotifications;
using Core.Block;
using Core.BlockStatus;
using Core.Ninja.Block;
using Core.ParseBlockCommand;
using Core.Transaction;

namespace InitialParser.Functions
{
    public class InitialParserFunctions
    {
        private readonly ILog _log;
        private readonly IConsole _console;
        private readonly INinjaBlockService _ninjaBlockService;
        private readonly IBlockStatusesRepository _blockStatusesRepository;
        private readonly IBlockService _blockService;
        private readonly ITransactionInputRepository _inputRepository;
        private readonly ITransactionOutputRepository _outputRepository;
        private readonly IProcessParseBlockCommandFacade _parseBlockCommandFacade;

        public InitialParserFunctions(IConsole console, 
            ILog log,
            INinjaBlockService ninjaBlockService, 
            IBlockStatusesRepository blockStatusesRepository, 
            IBlockService blockService,
            ITransactionInputRepository inputRepository, IProcessParseBlockCommandFacade parseBlockCommandFacade, ITransactionOutputRepository outputRepository)
        {
            _console = console;
            _log = log;
            _ninjaBlockService = ninjaBlockService;
            _blockStatusesRepository = blockStatusesRepository;
            _blockService = blockService;
            _inputRepository = inputRepository;
            _parseBlockCommandFacade = parseBlockCommandFacade;
            _outputRepository = outputRepository;
        }

        public async Task Run()
        {
            _console.WriteLine($"{nameof(InitialParserFunctions)}.{nameof(Run)} started");

            var getAllBlockStatuses = _blockStatusesRepository.GetHeights(BlockProcessingStatus.Done);
            var getTip = _ninjaBlockService.GetTip();

            await Task.WhenAll(getAllBlockStatuses, getTip);


            var blocksToExclude = getAllBlockStatuses.Result
                .ToDictionary(p => p);

            var blocksHeightsToParse = new List<int>();

            var startFromBlock = 460000;

            for (int height = startFromBlock; height <= getTip.Result.BlockHeight; height++)
            {
                if (!blocksToExclude.ContainsKey(height))
                {
                    blocksHeightsToParse.Add(height);
                }
            }
            var semaphore = new SemaphoreSlim(50);

            var cancellationTokenSource = new CancellationTokenSource();

            //StartSetNotFound(cancellationTokenSource.Token);

            var tasksToAwait = new List<Task>();
            var counter = blocksHeightsToParse.Count;
            foreach (var height in blocksHeightsToParse)
            {
                await semaphore.WaitAsync();

                tasksToAwait.Add(ParseBlock(height).ContinueWith(p =>
                {

                    counter--;
                    _console.WriteLine($"{counter} ");
                    semaphore.Release();
                }));
            }

            await Task.WhenAll(tasksToAwait);

            cancellationTokenSource.Cancel();

            await SetNotFounded();
        }

        private async Task ParseBlock(int height)
        {
            try
            {
                var header = await _ninjaBlockService.GetBlockHeader(height);

                await SetBlockStatus(header);

                await _parseBlockCommandFacade.ProcessCommand(
                    new ParseBlockCommandContext { BlockHeight = header.BlockHeight, BlockId = header.BlockId.ToString() });
            }
            catch (Exception e)
            {
                await _log.WriteErrorAsync(nameof(InitialParserFunctions), nameof(ParseBlock), height.ToString(), e);
            }
        }

        private async Task SetBlockStatus(INinjaBlockHeader header)
        {
            if (!await _blockStatusesRepository.Exists(header.BlockId.ToString()))
            {
                await _blockStatusesRepository.Insert(BlockStatus.Create(header.BlockHeight, header.BlockId.ToString(), BlockProcessingStatus.Queued,
                    queuedAt: DateTime.UtcNow, statusChangedAt: DateTime.UtcNow));
            }
        }

        
        private async Task SetNotFounded(int? itemsToTake = null)
        {
            _console.WriteLine($"{nameof(InitialParserFunctions)}.{nameof(SetNotFounded)} started");

            var notFoundInputs = await _inputRepository.Get(SpendProcessedStatus.NotFound, itemsToTake: itemsToTake);
            if (notFoundInputs.Any())
            {
                await _blockService.ProcessInputsToSpendable(notFoundInputs);
            }
        }
    }
}
