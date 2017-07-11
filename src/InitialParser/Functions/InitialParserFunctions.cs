using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Log;
using Core.AlertNotifications;
using Core.Block;
using Core.BlockStatus;
using Core.Ninja.Block;
using Core.ParseBlockCommand;
using Core.Settings;
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
        private readonly BaseSettings _baseSettings;
        
        public InitialParserFunctions(IConsole console, 
            ILog log,
            INinjaBlockService ninjaBlockService, 
            IBlockStatusesRepository blockStatusesRepository, 
            IBlockService blockService,
            ITransactionInputRepository inputRepository, 
            IProcessParseBlockCommandFacade parseBlockCommandFacade, 
            ITransactionOutputRepository outputRepository,
            BaseSettings baseSettings)
        {
            _console = console;
            _log = log;
            _ninjaBlockService = ninjaBlockService;
            _blockStatusesRepository = blockStatusesRepository;
            _blockService = blockService;
            _inputRepository = inputRepository;
            _parseBlockCommandFacade = parseBlockCommandFacade;
            _outputRepository = outputRepository;
            _baseSettings = baseSettings;
        }

        public async Task Run()
        {
            _console.WriteLine($"{nameof(InitialParserFunctions)}.{nameof(Run)} started");

            var getAllBlockStatuses = _blockStatusesRepository.GetHeights(BlockProcessingStatus.Done)
                .ContinueWith(p =>
                {
                    _console.WriteLine($"getAllBlockStatuses done");

                    return p.Result;
                });

            var ninjaGetTip = _ninjaBlockService.GetTip().ContinueWith(p =>
            {
                _console.WriteLine($"ninjaGetTip done");

                return p.Result;
            });

            await Task.WhenAll(getAllBlockStatuses, ninjaGetTip);

            var blocksToExclude = getAllBlockStatuses.Result
                .ToDictionary(p => p);

            var blocksHeightsToParse = new List<int>();

            var startFromBlock = _baseSettings.InitialParser?.StartFromBlockHeight ?? 1;

            for (int height = startFromBlock; height <= ninjaGetTip.Result.BlockHeight; height++)
            {
                if (!blocksToExclude.ContainsKey(height))
                {
                    blocksHeightsToParse.Add(height);
                }
            }
            var semaphore = new SemaphoreSlim(_baseSettings.InitialParser?.SemaphoreThreadCount??50);

            var cancellationTokenSource = new CancellationTokenSource();

            //StartSetNotFound(cancellationTokenSource.Token);

            var tasksToAwait = new List<Task>();
            var counter = blocksHeightsToParse.Count;
            var colorlock = new object();
            foreach (var height in blocksHeightsToParse)
            {
                await semaphore.WaitAsync();
                var st = new Stopwatch();
                st.Start();
                tasksToAwait.Add(ParseBlock(height).ContinueWith(p =>
                {

                    st.Stop();
                    counter--;
                    lock (colorlock)
                    {
                        int foregroundColor = (int)Console.ForegroundColor;
                        Console.ForegroundColor = ConsoleColor.Green;
                        _console.WriteLine($"{counter} remaining. elapsed {st.Elapsed.TotalSeconds} sec");
                        Console.ForegroundColor = (ConsoleColor) foregroundColor;
                    }
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
