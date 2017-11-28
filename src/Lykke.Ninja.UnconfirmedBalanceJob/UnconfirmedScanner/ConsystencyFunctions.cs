using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Common.Extensions;
using Common.Log;
using Lykke.JobTriggers.Triggers.Attributes;
using Lykke.Ninja.Core.UnconfirmedBalances.BalanceChanges;
using Lykke.Ninja.Core.UnconfirmedBalances.Statuses;

namespace Lykke.Ninja.UnconfirmedBalanceJob.UnconfirmedScanner
{
    public class ConsystencyFunctions
    {
	    private readonly IUnconfirmedStatusesRepository _unconfirmedStatusesRepository;
	    private readonly IUnconfirmedBalanceChangesRepository _unconfirmedBalanceChangesRepository;
	    private readonly ILog _log;

	    public ConsystencyFunctions(IUnconfirmedStatusesRepository unconfirmedStatusesRepository, 
			IUnconfirmedBalanceChangesRepository unconfirmedBalanceChangesRepository, ILog log)
	    {
		    _unconfirmedStatusesRepository = unconfirmedStatusesRepository;
		    _unconfirmedBalanceChangesRepository = unconfirmedBalanceChangesRepository;
		    _log = log;
	    }


	    [TimerTrigger("00:10:00")]
		public async Task CheckRemoved()
	    {
	        var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(5));
            try
		    {
			    await CheckRemovedInner(cancellationTokenSource.Token);
			}
		    catch (Exception e)
		    {
		        cancellationTokenSource.Cancel();
                await _log.WriteErrorAsync(nameof(ConsystencyFunctions), nameof(CheckRemoved), null, e);
		    }
	    }

	    private async Task CheckRemovedInner(CancellationToken cancellationToken)
	    {
		    var existedTxids = await _unconfirmedStatusesRepository.GetNotRemovedTxIds(InsertProcessStatus.Processed);
		    await _unconfirmedBalanceChangesRepository.RemoveExcept(existedTxids, cancellationToken);
	    }

	    [TimerTrigger("00:10:00")]
	    public async Task CheckExisted()
	    {
	        var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(5));
            try
		    {
				await CheckExistedInner(cancellationTokenSource.Token);
			}
		    catch (Exception e)
		    {
			    await _log.WriteErrorAsync(nameof(ConsystencyFunctions), nameof(CheckExisted), null, e);
		    }
		}

	    public async Task CheckExistedInner(CancellationToken cancellationToken)
	    {
			var existedTxidsFromStatuses = _unconfirmedStatusesRepository.GetNotRemovedTxIds(InsertProcessStatus.Processed);
		    var existedTxidsFromBalanceChanges = _unconfirmedBalanceChangesRepository.GetNotRemovedTxIds();

		    await Task.WhenAll(existedTxidsFromBalanceChanges, existedTxidsFromStatuses);

		    var existedBalanceChangesTxIds = existedTxidsFromBalanceChanges.Result.Distinct().ToDictionary(p => p);
		    var missedTxIds = existedTxidsFromStatuses.Result.Where(p => !existedBalanceChangesTxIds.ContainsKey(p));

		    await _unconfirmedStatusesRepository.SetInsertStatus(missedTxIds, InsertProcessStatus.Waiting, cancellationToken); // shall be processed on next update balance changes iteration
	    }
	}
}
