using System.Collections.Generic;
using System.Threading.Tasks;
using NBitcoin;

namespace Core.Transaction
{
    public interface ITransactionOutput
    {
        string Id { get; }
        string TransactionId { get; }

        ulong Index { get; }
        
        long BtcSatoshiAmount { get; }

        string BlockId { get; }

        int BlockHeight { get; }

        string DestinationAddress { get; }

        IColoredOutputData ColoredData { get; }

        ISpendTxInput SpendTxInput { get; }
    }

    public interface ISpendTxInput
    {
        string Id { get;  }

        ulong Index { get;  }

        string SpendedInTxId { get;  }

        string BlockId { get;  }

        int BlockHeight { get;  }
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
        //Task InsertIfNotExists(IEnumerable<ITransactionOutput> items);
        Task InsertIfNotExists(IEnumerable<ITransactionOutput> items, int blockHeight);

        Task<ISetSpendableOperationResult> SetSpended(IEnumerable<ITransactionInput> inputs);
        
        Task<long> GetTransactionsCount(BitcoinAddress address, int? at = null);
        
        Task<long> GetBtcAmountSummary(BitcoinAddress address, int? at = null, bool isColored = false);

        Task<long> GetBtcReceivedSummary(BitcoinAddress address, int? at = null, bool isColored = false);

        Task<IDictionary<string, long>> GetAssetsReceived(BitcoinAddress address, int? at = null);

        Task<IDictionary<string, long>> GetAssetsAmount(BitcoinAddress address, int? at = null);

        Task<IEnumerable<ITransactionOutput>> GetSpended(BitcoinAddress address, int? minBlockHeight = null, int? maxBlockHeight = null);

        Task<IEnumerable<ITransactionOutput>> GetReceived(BitcoinAddress address, bool unspendOnly, int? minBlockHeight = null, int? maxBlockHeight = null);

        Task SetIndexes();
        Task SetInsertionIndexes();
    }
}
