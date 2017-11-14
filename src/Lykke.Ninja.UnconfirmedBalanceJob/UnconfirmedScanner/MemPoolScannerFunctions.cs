using System.Linq;
using System.Threading.Tasks;
using Common.Extensions;
using Common.Log;
using Lykke.JobTriggers.Triggers.Attributes;
using Lykke.Ninja.Core.Bitcoin;
using Lykke.Ninja.Core.UnconfirmedBalances.BalanceChanges;
using Lykke.Ninja.Core.UnconfirmedBalances.Statuses;
using NBitcoin.RPC;

namespace Lykke.Ninja.UnconfirmedBalanceJob.UnconfirmedScanner
{

    public class MemPoolScannerFunctions
    {
        private readonly IBitcoinRpcClient _client;
        private readonly IUnconfirmedStatusesSinchronizeService _statusesSinchronizeService;
        private readonly IConsole _console;
        private readonly IUnconfirmedBalanceChangesCommandProducer _balanceChangesCommandProducer;

        public MemPoolScannerFunctions(IBitcoinRpcClient client,
            IUnconfirmedStatusesSinchronizeService statusesSinchronizeService,
            IConsole console,
            IUnconfirmedBalanceChangesCommandProducer balanceChangesCommandProducer)
        {
            _client = client;
            _statusesSinchronizeService = statusesSinchronizeService;
            _console = console;
            _balanceChangesCommandProducer = balanceChangesCommandProducer;
        }

        [TimerTrigger("00:00:10")]
        public async Task ScanUnconfirmed()
        {
            WriteConsole($"{nameof(ScanUnconfirmed)} started");

	        await ScanUnconfirmedInner().WithTimeout(20 * 60 * 1000);

			WriteConsole($"{nameof(ScanUnconfirmed)} done");
        }

        private async Task ScanUnconfirmedInner()
		{
			WriteConsole($"{nameof(ScanUnconfirmed)}. Check queue is full started");
			if (await _balanceChangesCommandProducer.IsQueueFull())
			{
				WriteConsole($"{nameof(ScanUnconfirmed)} Queue is full");
				return;
	        }

			WriteConsole($"{nameof(ScanUnconfirmed)}. GetUnconfirmedTransactionIds started");
			var txIds = (await _client.GetUnconfirmedTransactionIds()).ToList();
            WriteConsole($"{nameof(ScanUnconfirmed)}. GetUnconfirmedTransactionIds :: found {txIds.Count} unconfirmedTxs");

            var synchronizePlan =
                await _statusesSinchronizeService.GetStatusesSynchronizePlan(txIds.Select(p => p.ToString()));
            if (synchronizePlan.TxIdsToAdd.Any() || synchronizePlan.TxIdsToRemove.Any())
            {
                await _statusesSinchronizeService.Synchronize(synchronizePlan);
                await _balanceChangesCommandProducer.ProduceSynchronizeCommand();
            }
        }

        private void WriteConsole(string message)
        {
            _console.WriteLine($"{nameof(MemPoolScannerFunctions)} {message}");
        }
    }
}
