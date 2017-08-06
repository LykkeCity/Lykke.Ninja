using System.Collections.Generic;
using System.Threading.Tasks;
using NBitcoin;
using QBitNinja.Client.Models;

namespace Lykke.Ninja.Core.Ninja.Transaction
{
    public interface INinjaTransactionService
    {
        Task<GetTransactionResponse> Get(uint256 txId);
        Task<IEnumerable<GetTransactionResponse>> Get(IEnumerable<uint256> txIds);
    }
}
