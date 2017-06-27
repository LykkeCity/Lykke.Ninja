﻿using System.Collections.Generic;
using System.Linq;
using Core.Ninja.Contracts;

namespace Web.Models
{
    public class AddressSummaryViewModel: AddressSummaryContract
    {

        public static AddressSummaryViewModel Create(long transactionCount, 
            long btcAmount, 
            long btcReceived,
            IDictionary<string, long> assetsReceived,
            IDictionary<string, long> assetAmounts)
        {
            return new AddressSummaryViewModel
            {
                Unconfirmed = CreateInnerEmpty(),
                Immature = CreateInnerEmpty(),
                Confirmed = CreateInner(transactionCount, btcAmount, btcReceived, assetsReceived, assetAmounts),
                Spendable = CreateInner(transactionCount, btcAmount, btcReceived, assetsReceived, assetAmounts),
            };
        }

        private static AddressSummaryInnerContract CreateInnerEmpty()
        {
            return new AddressSummaryInnerContract
            {

                Assets = Enumerable.Empty<AddressSummaryInnerContract.AddressAssetContract>().ToArray()
            };
        }
        private static AddressSummaryInnerContract CreateInner(long transactionCount, 
            long btcAmount, 
            long btcReceived,
            IDictionary<string, long> assetsReceived,
            IDictionary<string, long> assetsAmounts)
        {
            return new AddressSummaryInnerContract
            {
                Balance = btcAmount,
                TotalTransactions = transactionCount,
                Received = btcReceived,
                Assets = assetsReceived.Select(p=> new AddressSummaryInnerContract.AddressAssetContract{AssetId = p.Key, Received = p.Value, Quantity = assetsAmounts[p.Key]}).ToArray()
            };
        }
    }
}