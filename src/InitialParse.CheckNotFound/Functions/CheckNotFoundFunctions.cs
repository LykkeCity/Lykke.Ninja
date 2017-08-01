using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Log;
using Core.Block;
using Core.BlockStatus;
using Core.Ninja.Block;
using Core.ParseBlockCommand;
using Core.Settings;
using Core.Transaction;

namespace InitialParse.CheckNotFound.Functions
{
    public class CheckNotFoundFunctions
    {
        private readonly ILog _log;
        private readonly IConsole _console;
        private readonly ITransactionInputRepository _inputRepository;
        private readonly ITransactionOutputRepository _outputRepository;
        private readonly BaseSettings _baseSettings;
        
        public CheckNotFoundFunctions(IConsole console, 
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
            _inputRepository = inputRepository;
            _outputRepository = outputRepository;
            _baseSettings = baseSettings;
        }

        public async Task Run()
        {

            
        }
    }
}
