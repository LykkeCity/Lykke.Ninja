﻿using System;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Lykke.Ninja.Core.BlockStatus;
using Lykke.Ninja.Core.Ninja.Block;
using Lykke.Ninja.Core.ParseBlockCommand;

namespace Lykke.Ninja.Services.PaseBlockCommand
{
    public class ParseBlockCommandsService: IParseBlockCommandsService
    {
        private readonly IParseBlockCommandProducer _commandProducer;
        private readonly INinjaBlockService _ninjaBlockService;
        private readonly IBlockStatusesRepository _blockStatusesRepository;
        private readonly ILog _log;
        private readonly IConsole _console;

        public ParseBlockCommandsService(IParseBlockCommandProducer commandProducer, 
            INinjaBlockService ninjaBlockService, 
            IBlockStatusesRepository blockStatusesRepository, 
            ILog log, 
            IConsole console)
        {
            _commandProducer = commandProducer;
            _ninjaBlockService = ninjaBlockService;
            _blockStatusesRepository = blockStatusesRepository;
            _log = log;
            _console = console;
        }

        public async Task ProduceParseBlockCommand(int blockHeight)
        {
            var blockHeader = await _ninjaBlockService.GetBlockHeader(blockHeight);

            var exists = await _blockStatusesRepository.Exists(blockHeader.BlockId.ToString());


            if (exists)
            {
                await _log.WriteWarningAsync(nameof(ParseBlockCommandsService),
                    nameof(ProduceParseBlockCommand),
                    new {blockId = blockHeader.BlockId.ToString()}.ToJson(),
                    "Attempt to add parse block command again");

                await _blockStatusesRepository.ChangeProcessingStatus(blockHeader.BlockId.ToString(),
                    BlockProcessingStatus.Queued);
;            }
            else
            {
                await _blockStatusesRepository.Insert(BlockStatus.Create(blockHeader.BlockHeight,
                    blockHeader.BlockId.ToString(),
                    BlockProcessingStatus.Queued,
                    DateTime.UtcNow,
                    DateTime.UtcNow));
            }

            await _commandProducer.CreateParseBlockCommand(blockHeader.BlockId.ToString(), blockHeader.BlockHeight);
            _console.WriteLine($"{nameof(ProduceParseBlockCommand)} Add to queue {blockHeight}");
        }

        public Task<int> GetQueuedCommandCount()
        {
            return _commandProducer.GetQueuedCommandCount();
        }

    }
}
