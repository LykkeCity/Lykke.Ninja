using System.Linq;
using System.Threading.Tasks;
using Common.Log;
using Lykke.JobTriggers.Triggers.Attributes;
using Lykke.Ninja.Core.Bitcoin;
using Lykke.Ninja.Core.UnconfirmedBalances.Statuses;
using NBitcoin.RPC;

namespace Lykke.Ninja.UnconfirmedBalanceJob.UnconfirmedScanner
{

    public class UnconfirmedScannerFunctions
    {
        private readonly IBitcoinRpcClient _client;
        private readonly IUnconfirmedTransactionStatusesService _statusesService;
        private readonly IConsole _console;

        public UnconfirmedScannerFunctions(IBitcoinRpcClient client, IUnconfirmedTransactionStatusesService statusesService, IConsole console)
        {
            _client = client;
            _statusesService = statusesService;
            _console = console;
        }

        [TimerTrigger("00:30:00")]
        public async Task ScanUnconfirmed()
        {
            _console.WriteLine($"{nameof(ScanUnconfirmed)} started");

            var txIds = (await _client.GetUnconfirmedTransactionIds()).ToList();
            _console.WriteLine($"{nameof(ScanUnconfirmed)}. {txIds.Count} unconfirmedTxs");

            var synchronizePlan = await _statusesService.GetSynchronizePlan(txIds.Select(p => p.ToString()));
            await _statusesService.Synchronize(synchronizePlan);

            _console.WriteLine($"{nameof(ScanUnconfirmed)} done");
        }
    }
}
