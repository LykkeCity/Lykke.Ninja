using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Lykke.Ninja.Core.Block;
using Lykke.Ninja.Core.BlockStatus;
using Lykke.Ninja.Core.Ninja.Block;
using Lykke.Ninja.Core.Ninja.Transaction;
using Lykke.Ninja.Core.ParseBlockCommand;
using NBitcoin;
using NBitcoin.OpenAsset;

namespace Lykke.Ninja.Services.PaseBlockCommand
{
    public class ProcessParseBlockCommandFacade : IProcessParseBlockCommandFacade
    {
        private readonly INinjaBlockService _ninjaBlockService;
        private readonly INinjaTransactionService _ninjaTransactionService;
        private readonly IBlockStatusesRepository _blockStatusesRepository;
        private readonly IBlockService _blockService;
        private readonly IConsole _console;
        private readonly ILog _log;

        public ProcessParseBlockCommandFacade(INinjaBlockService ninjaBlockService,
            INinjaTransactionService ninjaTransactionService,
            IBlockStatusesRepository blockStatusesRepository,
            IBlockService blockService,
            IConsole console, ILog log)
        {
            _ninjaBlockService = ninjaBlockService;
            _ninjaTransactionService = ninjaTransactionService;
            _blockStatusesRepository = blockStatusesRepository;
            _blockService = blockService;
            _console = console;
            _log = log;
        }

        public async Task ProcessCommand(ParseBlockCommandContext context)
        {
            try
            {
                WriteConsole(context.BlockHeight, "Started");

                var getBlock = _ninjaBlockService.GetBlock(uint256.Parse(context.BlockId))
                    .ContinueWith(p =>
                    {
                        WriteConsole(context.BlockHeight, "Get block done");
                        return p.Result;
                    });

                var setStartedStatus =
                    _blockStatusesRepository.ChangeProcessingStatus(context.BlockId, BlockProcessingStatus.Started).ContinueWith(p =>
                    {
                        WriteConsole(context.BlockHeight, "ChangeProcessingStatus block done");
                    });

                await Task.WhenAll(getBlock, setStartedStatus);
                
                WriteConsole(context.BlockHeight, "Get Transactions started");

                var coloredTransactions = await _ninjaTransactionService.Get(
                    getBlock.Result.Block.Transactions
                        .Where(p => p.HasValidColoredMarker())
                        .Select(p => p.GetHash()));
                
                WriteConsole(context.BlockHeight, "Get Transactions Done");

                WriteConsole(context.BlockHeight, "InsertDataInDb started");

                await _blockService.InsertDataInDb(getBlock.Result, coloredTransactions);
                
                WriteConsole(context.BlockHeight, "InsertDataInDb Done");

                WriteConsole(context.BlockHeight, "ChangeProcessingStatus started");

                await _blockStatusesRepository.ChangeProcessingStatus(context.BlockId, BlockProcessingStatus.Done);

                WriteConsole(context.BlockHeight, "ChangeProcessingStatus Done");

                WriteConsole(context.BlockHeight, "Done");
            }
            catch (Exception e)
            {
                await _blockStatusesRepository.ChangeProcessingStatus(context.BlockId, BlockProcessingStatus.Fail);

                throw;
            }
        }

        private void WriteConsole(int blockHeight, string message)
        {
            _console.WriteLine($"{nameof(ProcessParseBlockCommandFacade)}.{nameof(ProcessCommand)} Block Height:{blockHeight} {message}");

        }
    }
}
