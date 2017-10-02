﻿using System;
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
    public class AddressTransactionsViewModel: AddressTransactionListContract
    {
        public static AddressTransactionsViewModel Create(INinjaBlockHeader header, 
            Network network, 
            bool isColored,
            string continuationToken,
            IEnumerable<ITransactionOutput> spended, 
            IEnumerable<ITransactionOutput> received,
            IEnumerable<IBalanceChange> unconfirmedSpended,
            IEnumerable<IBalanceChange> unconfirmedReceived)
        {
            return new AddressTransactionsViewModel
            {
                ContinuationToken = continuationToken,
                Transactions = GetTxs(header, network, isColored, spended, received, unconfirmedSpended, unconfirmedReceived).ToArray(),
                ConflictedOperations = Enumerable.Empty<object>().ToArray()
            };
        }

        private static IEnumerable<AddressTransactionListItemContract> GetTxs(INinjaBlockHeader header, 
            Network network, 
            bool isColored,
            IEnumerable<ITransactionOutput> spended,
            IEnumerable<ITransactionOutput> received,
            IEnumerable<IBalanceChange> unconfirmedSpended,
            IEnumerable<IBalanceChange> unconfirmedReceived)
        {
            spended = spended ?? Enumerable.Empty<ITransactionOutput>();
            received = received ?? Enumerable.Empty<ITransactionOutput>();
            var scriptPubKeyDictionary = new Dictionary<string, string>();
            var mappedSpended = spended.Select(p => InOutViewModel.CreateSpend(p, isColored, network, scriptPubKeyDictionary)).ToList();
            var mappedReceived = received.Select(p => InOutViewModel.CreateReceived(p, isColored, network, scriptPubKeyDictionary)).ToList();

            var spendedLookup = mappedSpended.ToLookup(p => p.OperationTransactionId);
            var receivedLookup = mappedReceived.ToLookup(p => p.OperationTransactionId);

            
            var txIds = mappedSpended.Union(mappedReceived).Select(p=>p.OperationTransactionId).Distinct();

            return txIds.Select(txId =>
                    AddressTransactionViewModel.Create(
                        header,
                        isColored,
                        spendedLookup[txId], 
                        receivedLookup[txId]))
               .ToList()
               .OrderByDescending(p => p.Height)
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

        public static InOutViewModel CreateReceived(ITransactionOutput output, bool isColored, Network network, IDictionary<string, string> scriptPubKeyDictionary)
        {
            return Create(output.TransactionId, output.BlockId, output.BlockHeight, output, isColored, network, scriptPubKeyDictionary);
        }

        public static InOutViewModel CreateSpend(ITransactionOutput output, bool isColored, Network network, IDictionary<string, string> scriptPubKeyDictionary)
        {
            return Create(output.SpendTxInput.SpendedInTxId, output.SpendTxInput.BlockId, output.SpendTxInput.BlockHeight, output, isColored, network, scriptPubKeyDictionary);
        }

        private static InOutViewModel Create(string operationTransactionId, 
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
                IsColored = output.ColoredData != null
            };
        }

        //todo
        public static InOutViewModel CreateUnconfirmedSpend(
            IBalanceChange balanceChange,
            bool isColored,
            Network network,
            IDictionary<string, string> scriptPubKeyDictionary)
        {
            return CreateUnconfirmed(balanceChange, isColored, network, scriptPubKeyDictionary);
        }

        //todo
        public static InOutViewModel CreateUnconfirmedReceived(
            IBalanceChange balanceChange,
            bool isColored,
            Network network,
            IDictionary<string, string> scriptPubKeyDictionary)
        {
            return CreateUnconfirmed(balanceChange, isColored, network, scriptPubKeyDictionary);
        }

        //todo
        public static InOutViewModel CreateUnconfirmed(
            IBalanceChange balanceChange,
            bool isColored, 
            Network network,
            IDictionary<string, string> scriptPubKeyDictionary)
        {
            return new InOutViewModel
            {
                Address = balanceChange.Address,
                AssetId = isColored ? balanceChange.AssetId : null,
                Quantity = isColored ? (long?)balanceChange.AssetQuantity : null,
                TransactionId = balanceChange.TxId,
                Index = balanceChange.Index,
                Value = balanceChange.BtcSatoshiAmount,
                ScriptPubKey = GetPubKeyCached(balanceChange.Address, network, scriptPubKeyDictionary),
                OperationTransactionId = balanceChange.TxId,
                IsColored = balanceChange.HasColoredData
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
                IBase58Data b58 = Network.Parse<IBase58Data>(address, network);
                switch (b58.Type)
                {
                    case Base58Type.SCRIPT_ADDRESS:
                    case Base58Type.PUBKEY_ADDRESS:
                        return ((BitcoinAddress)b58).ScriptPubKey.ToHex();
                    case Base58Type.SECRET_KEY:
                        return ((BitcoinSecret)b58).ScriptPubKey.ToHex();
                    case Base58Type.COLORED_ADDRESS:
                        return ((BitcoinColoredAddress)b58).ScriptPubKey.ToHex();
                    case Base58Type.EXT_SECRET_KEY:
                        return ((BitcoinExtKey)b58).ScriptPubKey.ToHex();
                    case Base58Type.EXT_PUBLIC_KEY:
                        return ((BitcoinExtPubKey)b58).ScriptPubKey.ToHex();
                    default:
                        return null;

                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

        }
    }

    public class AddressTransactionViewModel: AddressTransactionListItemContract
    {
        public static AddressTransactionListItemContract Create(INinjaBlockHeader tipHeader,
            bool isColored, 
            IEnumerable<InOutViewModel> spended,
            IEnumerable<InOutViewModel> received)
        {

            var any = spended.FirstOrDefault() ?? received.First();

            var transactionId = any.OperationTransactionId;
            var blockId = any.OperationBlockId;
            var blockHeight = any.OperationBlockHeight;

            double amount;
            if (isColored)
            {
                amount = received.Where(p => !p.IsColored).Sum(p => p.Value) - spended.Where(p => !p.IsColored).Sum(p => p.Value);
            }
            else
            {
                amount = received.Sum(p => p.Value) - spended.Sum(p => p.Value);
            }
            return new AddressTransactionListItemContract
            {
                Amount = amount,
                TxId = transactionId,
                BlockId = blockId,
                Height = blockHeight,
                Confirmations = tipHeader.BlockHeight - blockHeight,
                Received = received.ToArray(),
                Spent = spended.ToArray(),
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
