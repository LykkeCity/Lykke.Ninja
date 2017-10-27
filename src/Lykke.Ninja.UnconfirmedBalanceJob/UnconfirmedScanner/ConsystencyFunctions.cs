using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Extensions;
using Lykke.JobTriggers.Triggers.Attributes;
using Lykke.Ninja.Core.UnconfirmedBalances.BalanceChanges;
using Lykke.Ninja.Core.UnconfirmedBalances.Statuses;

namespace Lykke.Ninja.UnconfirmedBalanceJob.UnconfirmedScanner
{
    public class ConsystencyFunctions
    {
	    private readonly IUnconfirmedStatusesRepository _unconfirmedStatusesRepository;
	    private readonly IUnconfirmedBalanceChangesRepository _unconfirmedBalanceChangesRepository;

	    public ConsystencyFunctions(IUnconfirmedStatusesRepository unconfirmedStatusesRepository, 
			IUnconfirmedBalanceChangesRepository unconfirmedBalanceChangesRepository)
	    {
		    _unconfirmedStatusesRepository = unconfirmedStatusesRepository;
		    _unconfirmedBalanceChangesRepository = unconfirmedBalanceChangesRepository;
	    }


	    [TimerTrigger("00:10:00")]
		public async Task CheckRemoved()
	    {
		    await CheckRemovedInner().WithTimeout(10 * 60 * 1000);
	    }

	    private async Task CheckRemovedInner()
	    {
		    var existedTxids = await _unconfirmedStatusesRepository.GetNotRemovedTxIds(InsertProcessStatus.Processed);
		    await _unconfirmedBalanceChangesRepository.RemoveExcept(existedTxids);
	    }

	    [TimerTrigger("00:10:00")]
	    public async Task CheckExisted()
	    {
		    await CheckExistedInner().WithTimeout(10 * 60 * 1000);
		}

	    public async Task CheckExistedInner()
	    {
			var existedTxidsFromStatuses = _unconfirmedStatusesRepository.GetNotRemovedTxIds(InsertProcessStatus.Processed);
		    var existedTxidsFromBalanceChanges = _unconfirmedBalanceChangesRepository.GetNotRemovedTxIds();

		    await Task.WhenAll(existedTxidsFromBalanceChanges, existedTxidsFromStatuses);

		    var existedBalanceChangesTxIds = existedTxidsFromBalanceChanges.Result.Distinct().ToDictionary(p => p);
		    var missedTxIds = existedTxidsFromStatuses.Result.Where(p => !existedBalanceChangesTxIds.ContainsKey(p));

		    await _unconfirmedStatusesRepository.SetInsertStatus(missedTxIds, InsertProcessStatus.Waiting); // shall be processed on next update balance changes iteration
	    }
	}
}
