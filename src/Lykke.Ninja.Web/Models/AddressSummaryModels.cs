using System.Collections.Generic;
using System.Linq;
using Lykke.Ninja.Core.Ninja.Contracts;
using Newtonsoft.Json;

namespace Lykke.Ninja.Web.Models
{
    public class AddressSummaryViewModel: AddressSummaryContract
    {
        private static long TxCountNotCalculatedMagicValue = -1;

        public static AddressSummaryViewModel Create(long? confirmedTransactionCount, 
            long? confirmedSpendedTransactionCount,
            long confirmedBtcAmount, 
            long confirmedBtcReceived,
            IReadOnlyDictionary<string, long> confirmedAssetsReceived,
            IReadOnlyDictionary<string, long> confirmedAssetAmounts,
            long unconfirmedTransactionCount,
            long unconfirmedBtcAmount,
            long unconfirmedBtcReceived,
            IReadOnlyDictionary<string, long> unconfirmedAssetsReceived,
            IReadOnlyDictionary<string, long> unconfirmedAssetAmounts)
        {
            return new AddressSummaryViewModel
            {
                Unconfirmed = CreateUnconfirmed(unconfirmedTransactionCount, unconfirmedBtcAmount, unconfirmedBtcReceived, 
                    unconfirmedAssetsReceived, unconfirmedAssetAmounts),
                Immature = CreateEmpty(),
                Confirmed = CreateConfirmed(confirmedTransactionCount, confirmedSpendedTransactionCount, confirmedBtcAmount, confirmedBtcReceived, confirmedAssetsReceived, confirmedAssetAmounts),
                Spendable = CreateSpendable(confirmedTransactionCount, unconfirmedTransactionCount, confirmedBtcAmount, unconfirmedBtcAmount,
                    confirmedBtcReceived, unconfirmedBtcReceived, confirmedAssetsReceived, unconfirmedAssetsReceived, confirmedAssetAmounts,
                    unconfirmedAssetAmounts)
            };
        }

        private static AddressBalanceSummaryInnerContract CreateEmpty()
        {
            return new AddressBalanceSummaryInnerContract
            {
                Assets = Enumerable.Empty<AddressBalanceSummaryInnerContract.AddressAssetContract>().ToArray()
            };
        }

        private static AddressSummaryInnderViewModel CreateConfirmed(long? totalTransactionCount,
            long? spendedTransactionCount,
            long btcAmount, 
            long btcReceived,
            IReadOnlyDictionary<string, long> assetsReceived,
            IReadOnlyDictionary<string, long> assetsAmounts)
        {
            long receivedTransactionsCount;
            if (totalTransactionCount != null && spendedTransactionCount != null)
            {
                receivedTransactionsCount = totalTransactionCount.Value - spendedTransactionCount.Value;
            }
            else
            {
                receivedTransactionsCount = TxCountNotCalculatedMagicValue;
            }

            var assetIds = assetsAmounts.Keys.Union(assetsAmounts.Keys).Distinct();

            return new AddressSummaryInnderViewModel
            {
                Balance = btcAmount,
                TotalTransactions = totalTransactionCount ?? TxCountNotCalculatedMagicValue,
                SpendedTransactions = spendedTransactionCount ?? TxCountNotCalculatedMagicValue,
                ReceivedTransactions = receivedTransactionsCount,
                Received = btcReceived,
                Assets = assetIds.Select(assetId => new AddressBalanceSummaryInnerContract.AddressAssetContract
                {
                    AssetId = assetId,
                    Received = assetsReceived.GetValueOrDefault(assetId, 0),
                    Quantity = assetsAmounts.GetValueOrDefault(assetId, 0)
                }).ToArray()
            };
        }

        private static AddressBalanceSummaryInnerContract CreateSpendable(
            long? totalConfirmedTransactionCount,
            long? totalUnconfirmedTransactionCount,
            long confirmedBtcAmount,
            long unconfirmedBtcAmount,
            long confirmedBtcReceived,
            long unconfirmedBtcReceived,
            IReadOnlyDictionary<string, long> confirmedAssetsReceived,
            IReadOnlyDictionary<string, long> unconfirmedAssetsReceived,
            IReadOnlyDictionary<string, long> confirmedAssetsAmounts,
            IReadOnlyDictionary<string, long> unconfirmedAssetsAmounts)
        {
            var assetIds = confirmedAssetsReceived.Keys.Union(unconfirmedAssetsReceived.Keys)
                .Union(confirmedAssetsAmounts.Keys)
                .Union(confirmedAssetsReceived.Keys)
                .Distinct();

            return new AddressBalanceSummaryInnerContract
            {
                Balance = confirmedBtcAmount + unconfirmedBtcAmount,
                TotalTransactions = (totalConfirmedTransactionCount + totalUnconfirmedTransactionCount) ?? TxCountNotCalculatedMagicValue,
                Received = confirmedBtcReceived + unconfirmedBtcReceived,
                Assets = assetIds.Select(assetId => new AddressBalanceSummaryInnerContract.AddressAssetContract
                {
                    AssetId = assetId,
                    Received = confirmedAssetsReceived.GetValueOrDefault(assetId, 0) + unconfirmedAssetsReceived.GetValueOrDefault(assetId, 0),
                    Quantity = confirmedAssetsAmounts.GetValueOrDefault(assetId, 0) + unconfirmedAssetsAmounts.GetValueOrDefault(assetId, 0)
                }).ToArray()
            };
        }

        private static AddressBalanceSummaryInnerContract CreateUnconfirmed(long totalTransactionCount,
            long btcAmount,
            long btcReceived,
            IReadOnlyDictionary<string, long> assetsReceived,
            IReadOnlyDictionary<string, long> assetsAmounts)
        {
            var assetIds = assetsAmounts.Keys.Union(assetsAmounts.Keys).Distinct();

            return new AddressBalanceSummaryInnerContract
            {
                Balance = btcAmount,
                TotalTransactions = totalTransactionCount,
                Received = btcReceived,
                Assets = assetIds.Select(assetId => new AddressBalanceSummaryInnerContract.AddressAssetContract
                {
                    AssetId = assetId,
                    Received = assetsReceived.GetValueOrDefault(assetId, 0),
                    Quantity = assetsAmounts.GetValueOrDefault(assetId, 0)
                }).ToArray()
            };
        }
    }


    public class AddressSummaryInnderViewModel : AddressSummaryContract.AddressBalanceSummaryInnerContract
    {
        [JsonProperty("spendedTransactionCount")]
        public long? SpendedTransactions { get; set; }

        [JsonProperty("receivedTransactionCount")]
        public long? ReceivedTransactions { get; set; }
    }
}
