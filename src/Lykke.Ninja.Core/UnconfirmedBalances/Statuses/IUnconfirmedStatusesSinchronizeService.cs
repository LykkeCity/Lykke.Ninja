using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lykke.Ninja.Core.UnconfirmedBalances.Statuses
{
    public interface IStatusesSynchronizePlan
    {
        IEnumerable<string> TxIdsToAdd { get; }

        IEnumerable<string> TxIdsToRemove { get; }
    }



    public interface IUnconfirmedStatusesSinchronizeService
    {
        Task<IStatusesSynchronizePlan> GetStatusesSynchronizePlan(IEnumerable<string> txIds);
        Task Synchronize(IStatusesSynchronizePlan plan, CancellationToken cancellationToken);
    }
}
