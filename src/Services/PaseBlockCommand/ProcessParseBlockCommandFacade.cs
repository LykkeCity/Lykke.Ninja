﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Log;
using Core.Block;
using Core.BlockStatus;
using Core.Ninja.Block;
using Core.Ninja.Transaction;
using Core.ParseBlockCommand;
using NBitcoin;
using NBitcoin.OpenAsset;

namespace Services.PaseBlockCommand
{
    public class ProcessParseBlockCommandFacade: IProcessParseBlockCommandFacade
    {
        private readonly INinjaBlockService _ninjaBlockService;
        private readonly INinjaTransactionService _ninjaTransactionService;
        private readonly IBlockStatusesRepository _blockStatusesRepository;
        private readonly IBlockService _blockService;
        private readonly IConsole _console;

        public ProcessParseBlockCommandFacade(INinjaBlockService ninjaBlockService, 
            INinjaTransactionService ninjaTransactionService, 
            IBlockStatusesRepository blockStatusesRepository, 
            IBlockService blockService, 
            IConsole console)
        {
            _ninjaBlockService = ninjaBlockService;
            _ninjaTransactionService = ninjaTransactionService;
            _blockStatusesRepository = blockStatusesRepository;
            _blockService = blockService;
            _console = console;
        }

        public async Task ProcessCommand(ParseBlockCommandContext context)
        {
            try
            {
                _console.WriteLine($"{nameof(ProcessCommand)} Block Height:{context.BlockHeight} Started");

                var getBlock = _ninjaBlockService.GetBlock(uint256.Parse(context.BlockId));
                var setStartedStatus = _blockStatusesRepository.ChangeProcessingStatus(context.BlockId, BlockProcessingStatus.Started);

                await Task.WhenAll(getBlock, setStartedStatus);

                _console.WriteLine($"{nameof(ProcessCommand)} Block Height:{context.BlockHeight} Get Transactions started");


                var coloredTransactions = await _ninjaTransactionService.Get(
                    getBlock.Result.Block.Transactions
                        .Where(p => p.HasValidColoredMarker())
                        .Select(p => p.GetHash()));

                _console.WriteLine($"{nameof(ProcessCommand)} Block Height:{context.BlockHeight} Insert data Started");

                await _blockService.Parse(getBlock.Result, coloredTransactions);

                await _blockStatusesRepository.ChangeProcessingStatus(context.BlockId, BlockProcessingStatus.Done);

                _console.WriteLine($"{nameof(ProcessCommand)} Block Height:{context.BlockHeight} Done");
            }
            catch (Exception e)
            {
                await _blockStatusesRepository.ChangeProcessingStatus(context.BlockId, BlockProcessingStatus.Fail);

                throw;
            }
        }
    }
}