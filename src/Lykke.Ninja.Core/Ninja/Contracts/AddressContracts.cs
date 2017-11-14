using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Lykke.Ninja.Core.Ninja.Contracts
{
    #region TransactionListContract

    public class TransactionListContract
    {
        [JsonProperty("continuation")]
        public string ContinuationToken { get; set; }

        [JsonProperty("operations")]
        public TransactionListItemContract[] Transactions { get; set; }

        [JsonProperty("conflictedOperations")]
        public object[] ConflictedOperations { get; set; }
    }

    public class TransactionListItemContract
    {
        [JsonProperty("amount", NullValueHandling =  NullValueHandling.Ignore)]
        public double? Amount { get; set; }

        [JsonProperty("confirmations")]
        public int Confirmations { get; set; }

        [JsonProperty("height")]
        public int? Height { get; set; }

        [JsonProperty("blockId")]
        public string BlockId { get; set; }

        [JsonProperty("transactionId")]
        public string TxId { get; set; }

        [JsonProperty("receivedCoins")]
        public InOutContract[] Received { get; set; }

        [JsonProperty("spentCoins")]
        public InOutContract[] Spent { get; set; }

        [JsonProperty("fees", NullValueHandling = NullValueHandling.Ignore)]
        public double? Fees { get; set; }
    }



    #endregion

    #region AddressSummaryContract

    public class AddressSummaryContract
    {
        [JsonProperty("unconfirmed")]
        public AddressBalanceSummaryInnerContract Unconfirmed { get; set; }

        [JsonProperty("confirmed")]
        public AddressBalanceSummaryInnerContract Confirmed { get; set; }

        [JsonProperty("spendable")]
        public AddressBalanceSummaryInnerContract Spendable { get; set; }

        [JsonProperty("immature")]
        public AddressBalanceSummaryInnerContract Immature { get; set; }

        public class AddressBalanceSummaryInnerContract
        {
            [JsonProperty("transactionCount")]
            public long TotalTransactions { get; set; }
            [JsonProperty("amount")]
            public long Balance { get; set; }
            [JsonProperty("received")]
            public long Received { get; set; }
            [JsonProperty("assets")]
            public AddressAssetContract[] Assets { get; set; }

            public class AddressAssetContract
            {
                [JsonProperty("asset")]
                public string AssetId { get; set; }
                [JsonProperty("quantity")]
                public long Quantity { get; set; }
                [JsonProperty("received")]
                public long Received { get; set; }
            }
        }
    }


    #endregion

    public class InOutContract
    {
        [JsonProperty("address")]
        public string Address { get; set; }

        [JsonProperty("transactionId")]
        public string TransactionId { get; set; }

        [JsonProperty("index")]
        public ulong Index { get; set; }

        [JsonProperty("value")]
        public double Value { get; set; }

        [JsonProperty("scriptPubKey")]
        public string ScriptPubKey { get; set; }

        [JsonProperty("redeemScript")]
        public string RedeemScript { get; set; }

        [JsonProperty("assetId")]
        public string AssetId { get; set; }

        [JsonProperty("quantity")]
        public double? Quantity { get; set; }
    }
}
