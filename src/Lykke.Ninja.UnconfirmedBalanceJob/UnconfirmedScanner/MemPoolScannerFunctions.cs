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

            if (!await _balanceChangesCommandProducer.IsQueueFull())
            {
                await ScanUnconfirmedInner().WithTimeout(10 * 60 * 1000);
            }
            else
            {
                WriteConsole($"{nameof(ScanUnconfirmed)} Queue is full");
            }

            WriteConsole($"{nameof(ScanUnconfirmed)} done");
        }

        private async Task ScanUnconfirmedInner()
        {
            var txIds = (await _client.GetUnconfirmedTransactionIds()).ToList();
            WriteConsole($"{nameof(ScanUnconfirmed)}. {txIds.Count} unconfirmedTxs");

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
