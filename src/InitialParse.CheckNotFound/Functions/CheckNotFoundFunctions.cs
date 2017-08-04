using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Log;
using Core.Block;
using Core.BlockStatus;
using Core.Ninja.Block;
using Core.Ninja.Transaction;
using Core.ParseBlockCommand;
using Core.Settings;
using Core.Transaction;
using NBitcoin;

namespace InitialParse.CheckNotFound.Functions
{
    public class CheckNotFoundFunctions
    {
        private readonly ILog _log;
        private readonly IConsole _console;
        private readonly ITransactionInputRepository _inputRepository;
        private readonly ITransactionOutputRepository _outputRepository;
        private readonly BaseSettings _baseSettings;
        private readonly INinjaTransactionService _ninjaTransactionService;
        private readonly INinjaBlockService _ninjaBlockService;
        private readonly IProcessParseBlockCommandFacade _parseBlockCommandFacade;
        private readonly IBlockStatusesRepository _blockStatusesRepository;

        public CheckNotFoundFunctions(IConsole console, 
            ILog log,
            INinjaBlockService ninjaBlockService, 
            IBlockStatusesRepository blockStatusesRepository, 
            IBlockService blockService,
            ITransactionInputRepository inputRepository, 
            IProcessParseBlockCommandFacade parseBlockCommandFacade, 
            ITransactionOutputRepository outputRepository,
            BaseSettings baseSettings,
            INinjaTransactionService ninjaTransactionService, 
            INinjaBlockService ninjaBlockService1, 
            IProcessParseBlockCommandFacade parseBlockCommandFacade1, IBlockStatusesRepository blockStatusesRepository1)
        {
            _console = console;
            _log = log;
            _inputRepository = inputRepository;
            _outputRepository = outputRepository;
            _baseSettings = baseSettings;
            _ninjaTransactionService = ninjaTransactionService;
            _ninjaBlockService = ninjaBlockService1;
            _parseBlockCommandFacade = parseBlockCommandFacade1;
            _blockStatusesRepository = blockStatusesRepository1;
        }

        public async Task Run()
        {
            var notFound = await _inputRepository.Get(SpendProcessedStatus.NotFound, int.MaxValue);
            var txIds = notFound.Select(p => p.TxIn.TransactionId).Distinct().ToList();

            var txs = await _ninjaTransactionService.Get(txIds.Select(uint256.Parse));

            var heights = txs.Select(p => p.Block.Height).Distinct().OrderBy(p=>p);

            var t = string.Join(" ", heights.Select(p => p.ToString()));
            var total = heights.Count();
            foreach (var height in heights)
            {
                total--;
                Console.WriteLine(total);
                await ParseBlock(height);
            }

        }

        private async Task ParseBlock(int height)
        {
            try
            {
                var header = await _ninjaBlockService.GetBlockHeader(height);
                
                await _parseBlockCommandFacade.ProcessCommand(
                    new ParseBlockCommandContext { BlockHeight = header.BlockHeight, BlockId = header.BlockId.ToString() });
            }
            catch (Exception e)
            {
                await _log.WriteErrorAsync(nameof(ParseBlock), nameof(ParseBlock), height.ToString(), e);
            }
        }
    }
}
