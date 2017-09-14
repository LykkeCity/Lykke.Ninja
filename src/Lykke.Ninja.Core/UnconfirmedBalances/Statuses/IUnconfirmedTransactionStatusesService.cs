using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Lykke.Ninja.Core.UnconfirmedBalances.Statuses
{
    public interface ISynchronizePlan
    {
        IEnumerable<string> TxIdsToAdd { get; }

        IEnumerable<string> TxIdsToRemove { get; }
    }
    public interface IUnconfirmedTransactionStatusesService
    {
        Task<ISynchronizePlan> GetSynchronizePlan(IEnumerable<string> txIds);
        Task Synchronize(ISynchronizePlan plan);
    }
}
