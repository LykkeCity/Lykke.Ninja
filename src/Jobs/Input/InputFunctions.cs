using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Core.Block;
using Core.Transaction;
using Lykke.JobTriggers.Triggers.Attributes;

namespace Jobs.Input
{
    public class InputFunctions
    {
        private readonly ITransactionInputRepository _inputRepository;
        private readonly IBlockService _blockService;
        private readonly ILog _log;
        private readonly IConsole _console;

        public InputFunctions(ITransactionInputRepository inputRepository, 
            IBlockService blockService, ILog log, IConsole console)
        {
            _inputRepository = inputRepository;
            _blockService = blockService;
            _log = log;
            _console = console;
        }


        [TimerTrigger("00:10:00")]
        public async Task SetNotFoundSpendable()
        {
            _console.WriteLine($"{nameof(InputFunctions)}.{nameof(SetNotFoundSpendable)} started");

            var inputs = await _inputRepository.Get(SpendProcessedStatus.NotFound, itemsToTake: 50000);
            if (inputs.Any())
            {
                await _log.WriteWarningAsync(nameof(InputFunctions), nameof(SetNotFoundSpendable), inputs.Take(5).ToJson(),
                    "Processing not found inputs");
                await _blockService.ProcessInputsToSpend(inputs);
            }
        }

        [TimerTrigger("01:00:00")]
        public async Task SetWaitingToSpend()
        {
            _console.WriteLine($"{nameof(InputFunctions)}.{nameof(SetWaitingToSpend)} started");

            var inputs = await _inputRepository.Get(SpendProcessedStatus.Waiting, itemsToTake: 50000);
            if (inputs.Any())
            {
                await _blockService.ProcessInputsToSpend(inputs);
            }
        }
    }
}
