﻿using System.Collections.Generic;
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

        bool HasColoredData { get; }

        string SpendTxId { get; }

        ulong? SpendTxInput { get; }
    }

    public static class BalanceChangeIdGenerator
    {
        public static string GenerateId(string txId, ulong index, string spendTxId)
        {
            return $"{txId}_{index}_{spendTxId}";
        }
    }
    

    public interface IUnconfirmedBalanceChangesRepository
    {
        Task Upsert(IEnumerable<IBalanceChange> items);

        Task Remove(IEnumerable<string> txIds);

        Task<long> GetTransactionsCount(string address);

        Task<long> GetSpendTransactionsCount(string address);

        Task<long> GetBtcAmountSummary(string address, bool isColored = false);

        Task<long> GetBtcReceivedSummary(string address, bool isColored = false);

        Task<IDictionary<string, long>> GetAssetsReceived(string address);

        Task<IDictionary<string, long>> GetAssetsAmount(string address);

        Task<IEnumerable<IBalanceChange>> GetSpended(string address);

        Task<IEnumerable<IBalanceChange>> GetReceived(string address);
    }
}
