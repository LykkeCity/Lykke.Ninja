using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Common.Extensions;
using Common.Log;
using Lykke.JobTriggers.Triggers.Attributes;
using Lykke.Ninja.Core.UnconfirmedBalances.BalanceChanges;

namespace Lykke.Ninja.UnconfirmedBalanceJob.UnconfirmedScanner
{
    public class UpdateExpirationFunctions
    {
        private readonly IConsole _console;
        private readonly IUnconfirmedBalanceChangesRepository _balanceChangesRepository;

        public UpdateExpirationFunctions(IConsole console, IUnconfirmedBalanceChangesRepository balanceChangesRepository)
        {
            _console = console;
            _balanceChangesRepository = balanceChangesRepository;
        }

        [TimerTrigger("00:03:00")]
        public async Task UpdateExpiration()
        {
            WriteConsole($"{nameof(UpdateExpiration)} started");

            await _balanceChangesRepository.UpdateExpiration().WithTimeout(10*60*1000);

            WriteConsole($"{nameof(UpdateExpiration)} done");
        }

        private void WriteConsole(string message)
        {
            _console.WriteLine($"{nameof(UnconfirmedBalanceChangesFunctions)} {message}");
        }
    }
}
