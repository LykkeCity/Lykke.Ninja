using System.Collections.Generic;
using System.Threading.Tasks;
using NBitcoin;

namespace Lykke.Ninja.Core.Bitcoin
{
    public interface IBitcoinRpcClient
    {
        Task<IEnumerable<uint256>> GetUnconfirmedTransactionIds(int timeoutSeconds = 10);
        Task<IEnumerable<NBitcoin.Transaction>> GetRawTransactions(IEnumerable<uint256> txIds, int timeoutSeconds = 10);
    }
}
