using System.Collections.Generic;
using System.Threading.Tasks;
using NBitcoin;

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
        Task InsertIfNotExists(IEnumerable<ITransactionOutput> items);
        Task<ISetSpendableOperationResult> SetSpended(IEnumerable<ITransactionInput> inputs);


        Task<long> GetTransactionsCount(BitcoinAddress address, int? at = null);


        Task<long> GetBtcAmount(BitcoinAddress address, int? at = null, bool isColored = false);

        Task<long> GetBtcReceived(BitcoinAddress address, int? at = null, bool isColored = false);

        Task<IDictionary<string, long>> GetAssetsReceived(BitcoinAddress address, int? at = null);

        Task<IDictionary<string, long>> GetAssetsAmount(BitcoinAddress address, int? at = null);
    }
}
