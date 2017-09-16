using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Lykke.Ninja.Core.UnconfirmedBalances.BalanceChanges
{
    public class BalanceChangeSynchronizeCommandContext
    {
    }
    public interface IUnconfirmedBalanceChangesCommandProducer
    {
        Task ProduceSynchronizeCommand();
    }
}
