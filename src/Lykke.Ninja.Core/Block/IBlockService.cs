using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Ninja.Core.Transaction;
using QBitNinja.Client.Models;

namespace Lykke.Ninja.Core.Block
{
    public interface IBlockService
    {
        Task InsertDataInDb(GetBlockResponse block, IEnumerable<GetTransactionResponse> coloredTransactions);
        Task<ISetSpendableOperationResult> ProcessInputsToSpend(IEnumerable<ITransactionInput> inputs);
    }
}
