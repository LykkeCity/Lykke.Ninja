using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Lykke.Ninja.Core.UnconfirmedBalances.BalanceChanges
{
    public interface IBalanceChangesSynchronizePlan
    {
        IEnumerable<string> TxIdsToAdd { get; }

        IEnumerable<string> TxIdsToRemove { get; }
    }

    public interface IUnconfirmedBalanceChangesSinchronizeService
    {
        Task<IBalanceChangesSynchronizePlan> GetBalanceChangesSynchronizePlan();
        Task Synchronyze(IBalanceChangesSynchronizePlan synchronizePlan);
    }
}
