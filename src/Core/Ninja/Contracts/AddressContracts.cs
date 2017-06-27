using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Core.Ninja.Contracts
{
    #region AddressTransactionListContract

    public class AddressTransactionListContract
    {
        [JsonProperty("continuation")]
        public string ContinuationToken { get; set; }

        [JsonProperty("operations")]
        public AddressTransactionListItemContract[] Transactions { get; set; }
    }

    public class AddressTransactionListItemContract
    {
        [JsonProperty("transactionId")]
        public string TxId { get; set; }

        [JsonProperty("amount")]
        public double Amount { get; set; }

        [JsonProperty("confirmations")]
        public int Confirmations { get; set; }

        [JsonProperty("height")]
        public int? Height { get; set; }

        [JsonProperty("blockId")]
        public string BlockId { get; set; }

        [JsonProperty("receivedCoins")]
        public InOutContract[] Received { get; set; }

        [JsonProperty("spentCoins")]
        public InOutContract[] Spent { get; set; }
    }



    #endregion

    #region AddressSummaryContract

    public class AddressSummaryContract
    {
        [JsonProperty("confirmed")]
        public AddressSummaryInnerContract Confirmed { get; set; }
        [JsonProperty("immature")]
        public AddressSummaryInnerContract Immature { get; set; }
        [JsonProperty("unconfirmed")]
        public AddressSummaryInnerContract Unconfirmed { get; set; }
        [JsonProperty("spendable")]
        public AddressSummaryInnerContract Spendable { get; set; }

        public class AddressSummaryInnerContract
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
        public int Index { get; set; }

        [JsonProperty("value")]
        public double Value { get; set; }

        [JsonProperty("scriptPubKey")]
        public string ScriptPubKey { get; set; }

        [JsonProperty("assetId")]
        public string AssetId { get; set; }

        [JsonProperty("quantity")]
        public double Quantity { get; set; }
    }
}
