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
        private readonly ILog _log;


        public UnconfirmedBalanceChangesFunctions(IUnconfirmedBalanceChangesSinchronizeService balanceChangesSinchronizeService, 
            IConsole console,
            ILog log)
        {
            _balanceChangesSinchronizeService = balanceChangesSinchronizeService;
            _console = console;
            _log = log;
        }
        
        [QueueTrigger(QueueNames.SynchronizeChanges, maxPollingIntervalMs: 5000, notify:true)]
        public async Task SynchronizeChanges(BalanceChangeSynchronizeCommandContext context)
        {
            await SynchronizeChanges().WithTimeout(60 * 20 * 1000);
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
                await _log.WriteErrorAsync(nameof(UnconfirmedBalanceChangesFunctions), nameof(SynchronizeChanges), null, e);
            }

        }

        private void WriteConsole(string message)
        {
            _console.WriteLine($"{nameof(UnconfirmedBalanceChangesFunctions)} {message}");
        }
    }
}
