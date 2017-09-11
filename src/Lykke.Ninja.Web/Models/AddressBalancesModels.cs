using System.Collections.Generic;
using System.Linq;
using Lykke.Ninja.Core.Ninja.Contracts;
using Newtonsoft.Json;

namespace Lykke.Ninja.Web.Models
{
    public class AddressSummaryViewModel: AddressSummaryContract
    {
        public static AddressSummaryViewModel Create(long? transactionCount, 
            long? spendedTransactionCount,
            long btcAmount, 
            long btcReceived,
            IDictionary<string, long> assetsReceived,
            IDictionary<string, long> assetAmounts)
        {
            return new AddressSummaryViewModel
            {
                Unconfirmed = CreateInnerEmpty(),
                Immature = CreateInnerEmpty(),
                Confirmed = CreateInner(transactionCount, spendedTransactionCount, btcAmount, btcReceived, assetsReceived, assetAmounts),
                Spendable = CreateInner(transactionCount, spendedTransactionCount, btcAmount, btcReceived, assetsReceived, assetAmounts),
            };
        }

        private static AddressBalanceSummaryInnerContract CreateInnerEmpty()
        {
            return new AddressBalanceSummaryInnerContract
            {
                Assets = Enumerable.Empty<AddressBalanceSummaryInnerContract.AddressAssetContract>().ToArray()
            };
        }

        private static AddressSummaryInnderViewModel CreateInner(long? totalTransactionCount,
            long? spendedTransactionCount,
            long btcAmount, 
            long btcReceived,
            IDictionary<string, long> assetsReceived,
            IDictionary<string, long> assetsAmounts)
        {
            var txCountNotCalculatedMagicValue = -1;

            long receivedTransactionsCount;
            if (totalTransactionCount != null && spendedTransactionCount != null)
            {
                receivedTransactionsCount = totalTransactionCount.Value - spendedTransactionCount.Value;
            }
            else
            {
                receivedTransactionsCount = txCountNotCalculatedMagicValue;
            }

            return new AddressSummaryInnderViewModel
            {
                Balance = btcAmount,
                TotalTransactions = totalTransactionCount ?? txCountNotCalculatedMagicValue,
                SpendedTransactions = spendedTransactionCount ?? txCountNotCalculatedMagicValue,
                ReceivedTransactions = receivedTransactionsCount,
                Received = btcReceived,
                Assets = assetsReceived.Select(p => new AddressBalanceSummaryInnerContract.AddressAssetContract
                {
                    AssetId = p.Key,
                    Received = p.Value,
                    Quantity = assetsAmounts.ContainsKey(p.Key) ? assetsAmounts[p.Key] : 0
                }).ToArray()
            };
        }
    }


    public class AddressSummaryInnderViewModel : AddressSummaryContract.AddressBalanceSummaryInnerContract
    {
        [JsonProperty("spendedTransactionCount")]
        public long SpendedTransactions { get; set; }

        [JsonProperty("receivedTransactionCount")]
        public long ReceivedTransactions { get; set; }
    }
}
