using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.Transaction
{
    public interface ITransactionOutput
    {
        string TransactionId { get; }

        uint Index { get; }

        long BtcSatoshiAmount { get; }

        string BlockId { get; }

        int BlockHeight { get; }

        string DestinationAddress { get; }
    }

    public interface ISetSpendableOperationResult
    {
        IEnumerable<ITransactionInput> Ok { get; }


        IEnumerable<ITransactionInput> NotFound { get; }
    }

    public interface ITransactionOutputRepository
    {
        Task Insert(IEnumerable<ITransactionOutput> outputs);
        Task<ISetSpendableOperationResult> SetSpendedBulk(IEnumerable<ITransactionInput> inputs);
    }
}
