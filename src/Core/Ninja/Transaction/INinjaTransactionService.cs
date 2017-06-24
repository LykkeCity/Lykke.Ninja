using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using NBitcoin;
using QBitNinja.Client.Models;

namespace Core.Ninja.Transaction
{
    public interface INinjaTransactionService
    {
        Task<GetTransactionResponse> Get(uint256 txId);
        Task<IEnumerable<GetTransactionResponse>> Get(IEnumerable<uint256> txIds);
    }
}
