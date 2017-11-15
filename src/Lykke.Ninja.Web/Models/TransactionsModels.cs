using System;
using System.Collections.Generic;
using System.Linq;
using Lykke.Ninja.Core.Ninja.Block;
using Lykke.Ninja.Core.Ninja.Contracts;
using Lykke.Ninja.Core.Transaction;
using Lykke.Ninja.Core.UnconfirmedBalances.BalanceChanges;
using NBitcoin;
using Newtonsoft.Json;
using Lykke.Ninja.Services.Ninja;

namespace Lykke.Ninja.Web.Models
{
    public class TransactionsViewModel: TransactionListContract
    {
        public static TransactionsViewModel Create(INinjaBlockHeader ninjaTop, 
            Network network, 
            bool isColored,
            IEnumerable<ITransactionOutput> spended, 
            IEnumerable<ITransactionOutput> received,
            string continuationToken = null,
            IEnumerable<IBalanceChange> unconfirmedSpended = null,
            IEnumerable<IBalanceChange> unconfirmedReceived = null,
            bool showFees = false,
            bool showAmount = true)
        {
            return new TransactionsViewModel
            {
                ContinuationToken = continuationToken,
                Transactions = GetTxs(ninjaTop, network, isColored, spended, received, unconfirmedSpended, unconfirmedReceived, showFees, showAmount).ToArray(),
                ConflictedOperations = Enumerable.Empty<object>().ToArray()
            };
        }

        private static IEnumerable<TransactionListItemContract> GetTxs(INinjaBlockHeader ninjaTop, 
            Network network, 
            bool isColored,
            IEnumerable<ITransactionOutput> spended,
            IEnumerable<ITransactionOutput> received,
            IEnumerable<IBalanceChange> unconfirmedSpended,
            IEnumerable<IBalanceChange> unconfirmedReceived,
            bool showFees,
            bool showAmount)
        {
            spended = spended ?? Enumerable.Empty<ITransactionOutput>();
            received = received ?? Enumerable.Empty<ITransactionOutput>();
            unconfirmedSpended = unconfirmedSpended ?? Enumerable.Empty<IBalanceChange>();
            unconfirmedReceived = unconfirmedReceived ?? Enumerable.Empty<IBalanceChange>();

            var scriptPubKeyDictionary = new Dictionary<string, string>();

            var mappedConfirmedSpended = spended
                .Select(p => InOutViewModel.CreateConfirmedSpend(p, isColored, network, scriptPubKeyDictionary)).ToList();
            var mappedUnconfirmedSpended = unconfirmedSpended
                .Select(p =>InOutViewModel.CreateUnconfirmedSpend(p, isColored, network, scriptPubKeyDictionary, ninjaTop)).ToList();

            var mappedConfirmedReceived = received
                .Select(p => InOutViewModel.CreateConfirmedReceived(p, isColored, network, scriptPubKeyDictionary)).ToList();
            var mappedUnconfirmedReceived = unconfirmedReceived
                .Select(p => InOutViewModel.CreateUnconfirmedReceived(p, isColored, network, scriptPubKeyDictionary, ninjaTop)).ToList();

            var mappedSpended = mappedConfirmedSpended.Union(mappedUnconfirmedSpended).ToList();
            var mappedReceived = mappedConfirmedReceived.Union(mappedUnconfirmedReceived).ToList();

            var spendedLookup = mappedSpended.ToLookup(p => p.OperationTransactionId);
            var receivedLookup = mappedReceived.ToLookup(p => p.OperationTransactionId);

            
            var txIds = mappedSpended.Union(mappedReceived).Select(p=>p.OperationTransactionId).Distinct();

            return txIds.Select(txId =>
                    TransactionViewModel.Create(
                        ninjaTop,
                        isColored,
                        spendedLookup[txId], 
                        receivedLookup[txId],
                        showFees,
                        showAmount))
               .ToList()
               .OrderBy(p => p.Confirmed)
               .ThenByDescending(p => p.Height)
               .ThenBy(p => p.Amount);
        }
    }

    public class InOutViewModel : InOutContract
    {
        [JsonIgnore]
        public string OperationTransactionId { get; set; }

        [JsonIgnore]
        public string OperationBlockId { get; set; }


        [JsonIgnore]
        public int OperationBlockHeight { get; set; }


        [JsonIgnore]
        public bool IsColored { get; set; }

        [JsonIgnore]
        public bool Confirmed { get; set; }

        public static InOutViewModel CreateConfirmedReceived(ITransactionOutput output, bool isColored, Network network, IDictionary<string, string> scriptPubKeyDictionary)
        {
            return CreateConfirmed(output.TransactionId, output.BlockId, output.BlockHeight, output, isColored, network, scriptPubKeyDictionary);
        }

        public static InOutViewModel CreateConfirmedSpend(ITransactionOutput output, bool isColored, Network network, IDictionary<string, string> scriptPubKeyDictionary)
        {
            return CreateConfirmed(output.SpendTxInput.SpendedInTxId, output.SpendTxInput.BlockId, output.SpendTxInput.BlockHeight, output, isColored, network, scriptPubKeyDictionary);
        }

        private static InOutViewModel CreateConfirmed(string operationTransactionId, 
            string operationBlockId, 
            int operationBlockHeight, 
            ITransactionOutput output, 
            bool isColored, 
            Network network, 
            IDictionary<string, string> scriptPubKeyDictionary)
        {
            return new InOutViewModel
            {
                Address = output.DestinationAddress,
                AssetId = isColored ? output.ColoredData?.AssetId : null,
                Quantity = isColored ? output.ColoredData?.Quantity : null,
                TransactionId = output.TransactionId,
                Index = output.Index,
                Value = output.BtcSatoshiAmount,
                ScriptPubKey = GetPubKeyCached(output.DestinationAddress, network, scriptPubKeyDictionary),
                OperationBlockHeight = operationBlockHeight,
                OperationBlockId = operationBlockId,
                OperationTransactionId = operationTransactionId,
                IsColored = output.ColoredData != null,
                Confirmed = true
            };
        }
        
        public static InOutViewModel CreateUnconfirmedSpend(
            IBalanceChange balanceChange,
            bool isColored,
            Network network,
            IDictionary<string, string> scriptPubKeyDictionary,
            INinjaBlockHeader ninjaTop)
        {
            return CreateUnconfirmed(balanceChange, 
                isColored, 
                network, 
                scriptPubKeyDictionary, 
                ninjaTop);
        }

        public static InOutViewModel CreateUnconfirmedReceived(
            IBalanceChange balanceChange,
            bool isColored,
            Network network,
            IDictionary<string, string> scriptPubKeyDictionary,
            INinjaBlockHeader ninjaTop)
        {
            return CreateUnconfirmed(balanceChange,
                isColored, 
                network, 
                scriptPubKeyDictionary,
                ninjaTop);
        }

        public static InOutViewModel CreateUnconfirmed(
            IBalanceChange balanceChange,
            bool isColored, 
            Network network,
            IDictionary<string, string> scriptPubKeyDictionary,
            INinjaBlockHeader ninjaTop)
        {
            var assetQuantity =
                balanceChange.IsInput ? balanceChange.AssetQuantity * (-1) : balanceChange.AssetQuantity;
            return new InOutViewModel
            {
                Address = balanceChange.Address,
                AssetId = isColored ? balanceChange.AssetId : null,
                Quantity = isColored ? (long?)assetQuantity : null,
                TransactionId = balanceChange.IsInput? balanceChange.SpendTxId:balanceChange.TxId,
                Index = balanceChange.Index,
                Value = balanceChange.IsInput ? balanceChange.BtcSatoshiAmount * (-1) : balanceChange.BtcSatoshiAmount,
                ScriptPubKey = GetPubKeyCached(balanceChange.Address, network, scriptPubKeyDictionary),
                OperationTransactionId = balanceChange.TxId,
                IsColored = balanceChange.HasColoredData,
                OperationBlockId = null,
                OperationBlockHeight = ninjaTop.BlockHeight,
                Confirmed = false
            };
        }

        private static string GetPubKeyCached(string address, 
            Network network, 
            IDictionary<string, string> scriptPubKeyDictionary)
        {
            if (string.IsNullOrEmpty(address))
            {
                return null;
            }

            if (scriptPubKeyDictionary.ContainsKey(address))
            {
                return scriptPubKeyDictionary[address];
            }

            var pubKey = GetPubKey(address, network);

            scriptPubKeyDictionary[address] = pubKey;

            return pubKey;
        }

        private static string GetPubKey(string address, Network network)
        {
            try
            {
                return BitcoinAddressHelper.GetBitcoinAddress(address, network).ScriptPubKey.ToHex();
            }
            catch (Exception e)
            {
                return null;
            }

        }
    }

    public class TransactionViewModel: TransactionListItemContract
    {
        [JsonIgnore]
        public bool Confirmed { get; set; }

        public static TransactionViewModel Create(INinjaBlockHeader tipHeader,
            bool isColored, 
            IEnumerable<InOutViewModel> spended,
            IEnumerable<InOutViewModel> received,
            bool showFees,
            bool showAmount)
        {
            var spendedArr = spended as InOutViewModel[] ?? spended.ToArray();
            var receivedArr = received as InOutViewModel[] ?? received.ToArray();
            var any = spendedArr.FirstOrDefault() ?? receivedArr.First();

            var transactionId = any.OperationTransactionId;
            var blockId = any.OperationBlockId;
            var blockHeight = any.OperationBlockHeight;
            var isConfirmed = any.Confirmed;

            double? amount = null;

            if (showAmount)
            {
                if (isColored)
                {
                    amount = receivedArr.Where(p => !p.IsColored).Sum(p => p.Value) - spendedArr.Where(p => !p.IsColored).Sum(p => p.Value);
                }
                else
                {
                    amount = receivedArr.Sum(p => p.Value) - spendedArr.Sum(p => p.Value);
                }
            }


            double? fees = null;

            if (showFees)
            {
                if (spendedArr.Any())
                {
                    fees = spendedArr.Sum(p => p.Value) - receivedArr.Sum(p => p.Value);
                }
                else
                {
                    fees = 0;
                }
            }
            return new TransactionViewModel
            {
                Amount = amount,
                TxId = transactionId,
                BlockId = blockId,
                Height = blockHeight,
                Confirmations = isConfirmed? (tipHeader.BlockHeight - blockHeight + 1) : 0,
                Received = receivedArr,
                Spent = spendedArr,
                Confirmed = isConfirmed,
                Fees = fees
            };
        }
    }

    public static class ContiniationBinder
    {
        public static int? GetItemsToSkipFromContinuationToke(string continuation)
        {
            int result;
            if (int.TryParse(continuation, out result))
            {
                return result;
            };

            return null;
        }

        public static string GetContinuationToken(int? itemsToSkip, int itemsToTake)
        {
            var nextSkip = (itemsToSkip ?? 0) + itemsToTake;
            return nextSkip.ToString();
        }
    }
    
}
