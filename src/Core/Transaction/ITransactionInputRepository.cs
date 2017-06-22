using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Core.Transaction
{
    public interface ITransactionInput
    {
        string TransactionId { get; }

        string BlockId { get; }

        int BlockHeight { get; }

        uint Index { get; }

        IInputTxIn InputTxIn { get; }

    }
    public interface IInputTxIn
    {
        string TransactionId { get; }

        uint Index { get; }
    }


    public interface ITransactionInputRepository
    {
        Task Insert(IEnumerable<ITransactionInput> inputs);
        Task SetSpendedProcessedBulk(ISetSpendableOperationResult operationResult);
    }
}
