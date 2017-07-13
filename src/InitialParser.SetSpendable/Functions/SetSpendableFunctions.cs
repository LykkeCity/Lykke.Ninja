using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Log;
using Core.Block;
using Core.BlockStatus;
using Core.Ninja.Block;
using Core.ParseBlockCommand;
using Core.Settings;
using Core.Transaction;

namespace InitialParser.SetSpendable.Functions
{
    public class SetSpendableFunctions
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
        
        public SetSpendableFunctions(IConsole console, 
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
            var needToStop = false;

            var itemsToTake = 50000;
            var total = await _inputRepository.Count(SpendProcessedStatus.Waiting);
            var counter = total;

            while (!needToStop)
            {

                var items = await _inputRepository.Get(SpendProcessedStatus.Waiting, itemsToTake);
                if (items.Any())
                {

                    var setSpendedResult = await _outputRepository.SetSpended(items);

                    await _inputRepository.SetSpended(setSpendedResult);
                }
                else
                {
                    needToStop = true;
                }
                counter -= itemsToTake;


                int foregroundColor = (int)Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Green;
                _console.WriteLine($"{counter} remaining");
                Console.ForegroundColor = (ConsoleColor)foregroundColor;
            }
        }
    }
}
