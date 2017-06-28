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
        public static AddressTransactionsViewModel Create(INinjaBlockHeader header, Network network, IEnumerable<ITransactionOutput> spended, IEnumerable<ITransactionOutput> received)
        {
            return new AddressTransactionsViewModel
            {
                ContinuationToken = null,
                Transactions = GetTxs(header, network, spended, received).ToArray()
            };
        }

        private static IEnumerable<AddressTransactionListItemContract> GetTxs(INinjaBlockHeader header, 
            Network network, 
            IEnumerable<ITransactionOutput> spended,
            IEnumerable<ITransactionOutput> received)
        {
            var mappedSpended = spended.Select(p => InOutViewModel.CreateSpend(p, network)).ToList();
            var mappedReceived = received.Select(p => InOutViewModel.CreateReceived(p, network)).ToList();

            var spendedLookup = mappedSpended.ToLookup(p => p.OperationTransactionId);
            var receivedLookup = mappedReceived.ToLookup(p => p.OperationTransactionId);

            
            var txIds = mappedSpended.Union(mappedReceived).Select(p=>p.OperationTransactionId).Distinct();

            return txIds.Select(txId =>
                    AddressTransactionViewModel.Create(
                        header,
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

        public static InOutViewModel CreateReceived(ITransactionOutput output, Network network)
        {
            return new InOutViewModel
            {
                Address = output.DestinationAddress,
                AssetId = output.ColoredData?.AssetId,
                TransactionId = output.TransactionId,
                Index = output.Index,
                Quantity = output.ColoredData?.Quantity,
                Value = output.BtcSatoshiAmount,
                ScriptPubKey = GetPubKey(output.DestinationAddress, network),
                OperationBlockHeight = output.BlockHeight,
                OperationBlockId = output.BlockId,
                OperationTransactionId = output.TransactionId
            };
        }

        public static InOutViewModel CreateSpend(ITransactionOutput output, Network network)
        {
            return new InOutViewModel
            {
                Address = output.DestinationAddress,
                AssetId = output.ColoredData?.AssetId,
                TransactionId = output.TransactionId,
                Index = output.Index,
                Quantity = output.ColoredData?.Quantity,
                Value = output.BtcSatoshiAmount,
                ScriptPubKey = GetPubKey(output.DestinationAddress, network),
                OperationBlockHeight = output.SpendTxInput.BlockHeight,
                OperationBlockId = output.SpendTxInput.BlockId,
                OperationTransactionId = output.SpendTxInput.SpendedInTxId
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
            IEnumerable<InOutViewModel> spended,
            IEnumerable<InOutViewModel> received)
        {

            var any = spended.FirstOrDefault() ?? received.First();

            var transactionId = any.OperationTransactionId;
            var blockId = any.OperationBlockId;
            var blockHeight = any.OperationBlockHeight;

            return new AddressTransactionListItemContract
            {
                Amount = received.Sum(p => p.Value) - spended.Sum(p => p.Value),
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
