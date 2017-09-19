using System.Collections.Generic;
using System.Threading.Tasks;
using NBitcoin;

namespace Lykke.Ninja.Core.Bitcoin
{
    public interface IBitcoinRpcClient
    {
        Task<IEnumerable<uint256>> GetUnconfirmedTransactionIds();
        Task<IEnumerable<NBitcoin.Transaction>> GetRawTransactions(IEnumerable<uint256> txIds);
    }
}
