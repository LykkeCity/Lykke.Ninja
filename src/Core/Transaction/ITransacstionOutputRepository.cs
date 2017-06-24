using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.Transaction
{
    public interface ITransactionOutput
    {
        string Id { get; }
        string TransactionId { get; }

        uint Index { get; }
        

        long BtcSatoshiAmount { get; }

        string BlockId { get; }

        int BlockHeight { get; }

        string DestinationAddress { get; }

        IColoredOutputData ColoredData { get; }
    }

    public interface IColoredOutputData
    {
        string AssetId { get; }

        long Quantity { get; }
    }

    public interface ISetSpendableOperationResult
    {
        IEnumerable<ITransactionInput> Ok { get; }


        IEnumerable<ITransactionInput> NotFound { get; }
    }

    public interface ITransactionOutputRepository
    {
        Task Insert(IEnumerable<ITransactionOutput> outputs);
        Task<ISetSpendableOperationResult> SetSpended(IEnumerable<ITransactionInput> inputs);
    }
}
