using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.Transaction
{
    public interface ITransactionOutput
    {
        string TransactionId { get; }

        uint OutputIndex { get; }

        long BtcSatoshiAmount { get; }

        string BlockId { get; }

        int BlockHeight { get; }

        string DestinationAddress { get; }
    }

    public interface ITransactionOutputRepository
    {
        Task Insert(IEnumerable<ITransactionOutput> outputs);
    }
}
