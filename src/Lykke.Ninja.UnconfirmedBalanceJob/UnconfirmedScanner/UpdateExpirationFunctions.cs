using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Extensions;
using Common.Log;
using Lykke.JobTriggers.Triggers.Attributes;
using Lykke.Ninja.Core.UnconfirmedBalances.BalanceChanges;
using Lykke.Ninja.Core.UnconfirmedBalances.Statuses;

namespace Lykke.Ninja.UnconfirmedBalanceJob.UnconfirmedScanner
{
    public class UpdateExpirationFunctions
    {
        private readonly IConsole _console;
        private readonly IUnconfirmedBalanceChangesRepository _balanceChangesRepository;
	    private readonly IUnconfirmedStatusesRepository _unconfirmedStatusesRepository;

        public UpdateExpirationFunctions(IConsole console, IUnconfirmedBalanceChangesRepository balanceChangesRepository, IUnconfirmedStatusesRepository unconfirmedStatusesRepository)
        {
            _console = console;
            _balanceChangesRepository = balanceChangesRepository;
	        _unconfirmedStatusesRepository = unconfirmedStatusesRepository;
        }

        [TimerTrigger("00:03:00")]
        public async Task UpdateExpiration()
        {
            WriteConsole($"{nameof(UpdateExpiration)} started");
	        var txIds = (await _unconfirmedStatusesRepository.GetNotRemovedTxIds(InsertProcessStatus.Processed)).ToList();

            await _balanceChangesRepository.UpdateExpiration(txIds).WithTimeout(10 * 60 * 1000);

			WriteConsole($"{nameof(UpdateExpiration)} done");
        }

        private void WriteConsole(string message)
        {
            _console.WriteLine($"{nameof(UnconfirmedBalanceChangesFunctions)} {message}");
        }
    }
}
