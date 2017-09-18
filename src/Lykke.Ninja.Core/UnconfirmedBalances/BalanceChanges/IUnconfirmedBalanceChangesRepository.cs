using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Ninja.Core.UnconfirmedBalances.Statuses;
using NBitcoin;

namespace Lykke.Ninja.Core.UnconfirmedBalances.BalanceChanges
{
    public interface IBalanceChange
    {
        string Id { get; }
        string TxId { get; }
        ulong Index { get; }
        bool IsInput { get; }
        long BtcSatoshiAmount { get; }
        string Address { get; }

        string AssetId { get; }

        long AssetQuantity { get; }
    }

    public static class BalanceChangeIdGenerator
    {
        public static string GenerateId(string txId, ulong index)
        {
            return $"{txId}_{index}";
        }
        public static string GetTxId(string id)
        {
            if (id != null && id.Contains("_"))
            {
                return id.Split("_".ToCharArray())[0];
            }

            return null;
        }
    }
    

    public interface IUnconfirmedBalanceChangesRepository
    {
        Task Upsert(IEnumerable<IBalanceChange> items);

        Task Remove(IEnumerable<string> txIds);

        Task<long> GetTransactionsCount(string address, int? at = null);

        Task<long> GetSpendTransactionsCount(string address, int? at = null);

        Task<long> GetBtcAmountSummary(string address, int? at = null, bool isColored = false);

        Task<long> GetBtcReceivedSummary(string address, int? at = null, bool isColored = false);

        Task<IDictionary<string, long>> GetAssetsReceived(string address, int? at = null);

        Task<IDictionary<string, long>> GetAssetsAmount(string address, int? at = null);

        Task<IEnumerable<IBalanceChange>> GetSpended(string address,
            int? minBlockHeight = null,
            int? maxBlockHeight = null,
            int? itemsToSkip = null,
            int? itemsToTake = null);

        Task<IEnumerable<IBalanceChange>> GetReceived(string address,
            bool unspendOnly,
            int? minBlockHeight = null,
            int? maxBlockHeight = null,
            int? itemsToSkip = null,
            int? itemsToTake = null);
    }
}
