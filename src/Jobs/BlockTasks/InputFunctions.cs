using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Core.Block;
using Core.Transaction;
using Lykke.JobTriggers.Triggers.Attributes;

namespace Jobs.BlockTasks
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


        [TimerTrigger("00:00:30")]
        public async Task SetNotFound()
        {
            _console.WriteLine($"{nameof(InputFunctions)}.{nameof(SetNotFound)} started");

            var notFoundInputs = await _inputRepository.Get(SpendProcessedStatus.NotFound);
            if (notFoundInputs.Any())
            {
                await _log.WriteWarningAsync(nameof(InputFunctions), nameof(SetNotFound), notFoundInputs.Take(5).ToJson(),
                    "Processing not found inputs");
                await _blockService.ProcessInputsToSpendable(notFoundInputs);
            }
        }
    }
}
