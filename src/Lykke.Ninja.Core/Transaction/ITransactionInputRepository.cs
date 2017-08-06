using System.Collections.Generic;
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

    public enum SpendProcessedStatus
    {
        Waiting,
        Ok,
        NotFound
    }

    public interface ITransactionInputRepository
    {
        Task InsertIfNotExists(IEnumerable<ITransactionInput> items, int blockHeight);
        Task SetSpended(ISetSpendableOperationResult operationResult);
        Task<IEnumerable<ITransactionInput>> Get(SpendProcessedStatus status, 
            int itemsToTake = 5000,
            int? itemsToSkip = null);
        
        Task<long> Count(SpendProcessedStatus status);
    }
}
