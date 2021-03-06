﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Ninja.Core.Block;
using Lykke.Ninja.Core.BlockStatus;
using Lykke.Ninja.Core.Ninja.Block;
using Lykke.Ninja.Core.ParseBlockCommand;
using Lykke.Ninja.Core.Settings;
using Lykke.Ninja.Core.Transaction;

namespace InitialParser.SetSpendable.Functions
{
    public class SetSpendableFunctions
    {
        private readonly ILog _log;
        private readonly IConsole _console;
        private readonly ITransactionInputRepository _inputRepository;
        private readonly ITransactionOutputRepository _outputRepository;
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
            _inputRepository = inputRepository;
            _outputRepository = outputRepository;
            _baseSettings = baseSettings;
        }

        public async Task Run()
        {
            var threadsCount = _baseSettings.InitialParser?.SetSpendable?.ThreadsCount ?? 5;
            var itemsToTake = _baseSettings.InitialParser?.SetSpendable?.BatchSize ?? 50000;
            var total = await _inputRepository.Count(SpendProcessedStatus.Waiting);
            var counter = total;

            var needToStop = false;
            while (!needToStop)
            {
                var batch = new List<Task>();
                var items = new ConcurrentBag<ITransactionInput>();
                for (int i = 0; i < threadsCount; i++)
                {
                    var tsk = _inputRepository.Get(SpendProcessedStatus.Waiting, itemsToTake, itemsToSkip: i* itemsToTake)
                        .ContinueWith(async p =>
                        {
                            var opResult = await _outputRepository.SetSpended(p.Result);
                            await _inputRepository.SetSpended(opResult);
                            foreach (var elem in p.Result)
                            {
                                items.Add(elem);
                            }
                        });
                    batch.Add(tsk.Unwrap());
                }

                await Task.WhenAll(batch);

                if (!items.Any())
                {
                    needToStop = true;
                }

                counter -= items.Count;

                var foregroundColor = (int)Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Green;
                _console.WriteLine($"{counter} remaining");
                Console.ForegroundColor = (ConsoleColor)foregroundColor;
            }
        }
    }
}
