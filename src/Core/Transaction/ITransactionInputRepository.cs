using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Core.Transaction
{
    public interface ITransactionInput
    {
        string Id { get; }

        string TransactionId { get; }

        string BlockId { get; }

        int BlockHeight { get; }

        uint Index { get; }

        IInputTxIn TxIn { get; }

    }
    public interface IInputTxIn
    {
        string Id { get; }
        string TransactionId { get; }

        uint Index { get; }
    }


    public interface ITransactionInputRepository
    {
        Task Insert(IEnumerable<ITransactionInput> inputs);
        Task SetSpended(ISetSpendableOperationResult operationResult);
    }
}
