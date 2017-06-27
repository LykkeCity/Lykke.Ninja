using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Transaction;
using QBitNinja.Client.Models;

namespace Core.Block
{
    public interface IBlockService
    {
        Task Parse(GetBlockResponse block, IEnumerable<GetTransactionResponse> coloredTransactions);
        Task ProcessInputsToSpendable(IEnumerable<ITransactionInput> inputs);
    }
}
