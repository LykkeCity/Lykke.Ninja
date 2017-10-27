using System;
using System.Collections.Generic;
using System.Text;
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
    }
}
