using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Extensions;
using Common.Log;
using Lykke.JobTriggers.Triggers.Attributes;
using Lykke.Ninja.Core.Queue;
using Lykke.Ninja.Core.UnconfirmedBalances.BalanceChanges;

namespace Lykke.Ninja.UnconfirmedBalanceJob.UnconfirmedScanner
{
    public class UnconfirmedBalanceChangesFunctions
    {
        private readonly IUnconfirmedBalanceChangesSinchronizeService _balanceChangesSinchronizeService;
        private readonly IConsole _console;

        public UnconfirmedBalanceChangesFunctions(IUnconfirmedBalanceChangesSinchronizeService balanceChangesSinchronizeService, 
            IConsole console)
        {
            _balanceChangesSinchronizeService = balanceChangesSinchronizeService;
            _console = console;
        }
        
        [QueueTrigger(QueueNames.SynchronizeChanges, maxPollingIntervalMs: 5000)]
        public async Task SynchronizeChanges(BalanceChangeSynchronizeCommandContext context)
        {
            await SynchronizeChanges().WithTimeout(60 * 1000);
        }

        private async Task SynchronizeChanges()
        {
            WriteConsole($"{nameof(SynchronizeChanges)} started");

            var synchronizePlan = await _balanceChangesSinchronizeService.GetBalanceChangesSynchronizePlan();

            WriteConsole($"Synchronize started {synchronizePlan.TxIdsToRemove.Count()} items to remove, {synchronizePlan.TxIdsToAdd.Count()} items to add");

            await _balanceChangesSinchronizeService.Synchronize(synchronizePlan);

            WriteConsole($"{nameof(SynchronizeChanges)} done");
        }

        private void WriteConsole(string message)
        {
            _console.WriteLine($"{nameof(UnconfirmedBalanceChangesFunctions)} {message}");
        }
    }
}
