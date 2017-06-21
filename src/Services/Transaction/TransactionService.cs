using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.Settings;
using Core.Transaction;
using NBitcoin;
using QBitNinja.Client.Models;
using Services.Settings;

namespace Services.Transaction
{

    public class TransactionOutput : ITransactionOutput
    {
        public string TransactionId { get; set; }
        public uint OutputIndex { get; set; }
        public long BtcSatoshiAmount { get; set; }
        public string BlockId { get; set; }
        public int BlockHeight { get; set; }
        public string DestinationAddress { get; set; }

        public static IEnumerable<TransactionOutput> Create(NBitcoin.Transaction transaction, BlockInformation blockInformation, Network network)
        {
            return transaction.Outputs.AsIndexedOutputs().Select(output=> Create(output, blockInformation, transaction, network));
        }

        public static TransactionOutput Create(NBitcoin.IndexedTxOut output, BlockInformation blockInformation, NBitcoin.Transaction transaction, Network network)
        {
            return new TransactionOutput
            {
                BlockHeight = blockInformation.Height,
                BlockId = blockInformation.BlockId.ToString(),
                BtcSatoshiAmount = output.ToCoin().Amount.Satoshi,
                OutputIndex = output.N,
                TransactionId = transaction.GetHash().ToString(),
                DestinationAddress = output.TxOut.ScriptPubKey.GetDestinationAddress(network)?.ToWif()
            };
        }
    }

    public class TransactionService: ITransactionService
    {
        private readonly ITransactionOutputRepository _outputRepository;
        private readonly Network _network;

        public TransactionService(ITransactionOutputRepository outputRepository, BaseSettings baseSettings)
        {
            _outputRepository = outputRepository;
            _network = baseSettings.UsedNetwork();
        }

        public Task Insert(GetBlockResponse block)
        {
            return _outputRepository.Insert(block.Block.Transactions.SelectMany(transaction => TransactionOutput.Create(transaction, block.AdditionalInformation, _network)));
        }
    }
}
