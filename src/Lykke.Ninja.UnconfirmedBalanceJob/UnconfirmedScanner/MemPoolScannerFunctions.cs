using System;
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
        private readonly IUnconfirmedBalanceChangesSinchronizeService _balanceChangesSinchronizeService;
        private readonly ILog _log;

        public MemPoolScannerFunctions(IBitcoinRpcClient client,
            IUnconfirmedStatusesSinchronizeService statusesSinchronizeService,
            IConsole console,
            IUnconfirmedBalanceChangesSinchronizeService balanceChangesSinchronizeService, 
            ILog log)
        {
            _client = client;
            _statusesSinchronizeService = statusesSinchronizeService;
            _console = console;
            _balanceChangesSinchronizeService = balanceChangesSinchronizeService;
            _log = log;
        }

        [TimerTrigger("00:02:00")]
        public async Task SyncMempoolTransactions()
        {
            await ScanMempoolTransactions().WithTimeout(5 * 60 * 1000);

            await SynchronizeChanges().WithTimeout(20 * 60 * 1000);
        }

        private async Task ScanMempoolTransactions()
        {
            WriteConsole($"{nameof(ScanMempoolTransactions)} started");

			WriteConsole($"{nameof(ScanMempoolTransactions)}. GetUnconfirmedTransactionIds started");
			var txIds = (await _client.GetUnconfirmedTransactionIds()).ToList();
            WriteConsole($"{nameof(ScanMempoolTransactions)}. GetUnconfirmedTransactionIds :: found {txIds.Count} unconfirmedTxs");

            var synchronizePlan =
                await _statusesSinchronizeService.GetStatusesSynchronizePlan(txIds.Select(p => p.ToString()));
            if (synchronizePlan.TxIdsToAdd.Any() || synchronizePlan.TxIdsToRemove.Any())
            {
                await _statusesSinchronizeService.Synchronize(synchronizePlan);
            }

            WriteConsole($"{nameof(ScanMempoolTransactions)} done");
        }

        private async Task SynchronizeChanges()
        {
            WriteConsole($"{nameof(SynchronizeChanges)} started");

            try
            {
                var synchronizePlan = await _balanceChangesSinchronizeService.GetBalanceChangesSynchronizePlan();

                WriteConsole($"Synchronyze started {synchronizePlan.TxIdsToRemove.Count()} items to remove, {synchronizePlan.TxIdsToAdd.Count()} items to add");

                await _balanceChangesSinchronizeService.Synchronyze(synchronizePlan);

                WriteConsole($"{nameof(SynchronizeChanges)} done");
            }
            catch (Exception e)
            {
                await _log.WriteErrorAsync(nameof(MemPoolScannerFunctions), nameof(SynchronizeChanges), null, e);
            }

        }

        private void WriteConsole(string message)
        {
            _console.WriteLine($"{nameof(MemPoolScannerFunctions)} {message}");
        }
    }
}
