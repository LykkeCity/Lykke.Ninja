using System.Linq;
using System.Threading.Tasks;
using Common.Log;
using Lykke.JobTriggers.Triggers.Attributes;
using Lykke.Ninja.Core.Bitcoin;
using Lykke.Ninja.Core.UnconfirmedBalances.BalanceChanges;
using Lykke.Ninja.Core.UnconfirmedBalances.Statuses;
using NBitcoin.RPC;

namespace Lykke.Ninja.UnconfirmedBalanceJob.UnconfirmedScanner
{

    public class UnconfirmedStatusesFunctions
    {
        private readonly IBitcoinRpcClient _client;
        private readonly IUnconfirmedStatusesSinchronizeService _statusesSinchronizeService;
        private readonly IConsole _console;
        private readonly IUnconfirmedBalanceChangesCommandProducer _balanceChangesCommandProducer;

        public UnconfirmedStatusesFunctions(IBitcoinRpcClient client,
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
            _console.WriteLine($"{nameof(ScanUnconfirmed)} started");

            var txIds = (await _client.GetUnconfirmedTransactionIds()).ToList();
            _console.WriteLine($"{nameof(ScanUnconfirmed)}. {txIds.Count} unconfirmedTxs");

            var synchronizePlan = await _statusesSinchronizeService.GetStatusesSynchronizePlan(txIds.Select(p => p.ToString()));
            if (synchronizePlan.TxIdsToAdd.Any() || synchronizePlan.TxIdsToRemove.Any())
            {
                await _statusesSinchronizeService.Synchronize(synchronizePlan);
                await _balanceChangesCommandProducer.ProduceSynchronizeCommand();
            }

            _console.WriteLine($"{nameof(ScanUnconfirmed)} done");
        }
    }
}
