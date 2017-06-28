using System;
using System.Collections.Generic;
using System.Linq;
using Core.Ninja.Block;
using Core.Ninja.Contracts;
using Core.Transaction;
using NBitcoin;
using Newtonsoft.Json;
using Services.Ninja;

namespace Web.Models
{
    public class AddressTransactionsViewModel: AddressTransactionListContract
    {
        public static AddressTransactionsViewModel Create(INinjaBlockHeader header, 
            Network network, 
            bool isColored,
            IEnumerable<ITransactionOutput> spended = null, 
            IEnumerable<ITransactionOutput> received = null)
        {
            return new AddressTransactionsViewModel
            {
                ContinuationToken = null,
                Transactions = GetTxs(header, network, isColored, spended, received).ToArray(),
                ConflictedOperations = Enumerable.Empty<object>().ToArray()
            };
        }

        private static IEnumerable<AddressTransactionListItemContract> GetTxs(INinjaBlockHeader header, 
            Network network, 
            bool isColored,
            IEnumerable<ITransactionOutput> spended = null,
            IEnumerable<ITransactionOutput> received = null)
        {
            spended = spended ?? Enumerable.Empty<ITransactionOutput>();
            received = received ?? Enumerable.Empty<ITransactionOutput>();

            var mappedSpended = spended.Select(p => InOutViewModel.CreateSpend(p, isColored, network)).ToList();
            var mappedReceived = received.Select(p => InOutViewModel.CreateReceived(p, isColored, network)).ToList();

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

        public static InOutViewModel CreateReceived(ITransactionOutput output, bool isColored, Network network)
        {
            return Create(output.TransactionId, output.BlockId, output.BlockHeight, output, isColored, network);
        }

        public static InOutViewModel CreateSpend(ITransactionOutput output, bool isColored, Network network)
        {
            return Create(output.SpendTxInput.SpendedInTxId, output.SpendTxInput.BlockId, output.SpendTxInput.BlockHeight, output, isColored, network);
        }

        private static InOutViewModel Create(string operationTransactionId, 
            string operationBlockId, 
            int operationBlockHeight, 
            ITransactionOutput output, 
            bool isColored, 
            Network network)
        {
            return new InOutViewModel
            {
                Address = output.DestinationAddress,
                AssetId = isColored ? (output.ColoredData?.AssetId) : null,
                Quantity = isColored ? (output.ColoredData?.Quantity) : null,
                TransactionId = output.TransactionId,
                Index = output.Index,
                Value = output.BtcSatoshiAmount,
                ScriptPubKey = GetPubKey(output.DestinationAddress, network),
                OperationBlockHeight = operationBlockHeight,
                OperationBlockId = operationBlockId,
                OperationTransactionId = operationTransactionId,
                IsColored = output.ColoredData != null
            };
        }

        private static string GetPubKey(string address, Network network)
        {
            return address != null
                ? BitcoinAddressHelper.GetBitcoinAddress(address, network).ScriptPubKey.ToHex()
                : null;
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
    
}
