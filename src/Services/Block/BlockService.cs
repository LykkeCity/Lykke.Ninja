using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Block;
using Core.Settings;
using Core.Transaction;
using NBitcoin;
using QBitNinja.Client.Models;
using Services.Settings;

namespace Services.Block
{

    #region  TransactionOutput

    public class TransactionOutput : ITransactionOutput
    {
        public string TransactionId { get; set; }
        public uint Index { get; set; }
        public long BtcSatoshiAmount { get; set; }
        public string BlockId { get; set; }
        public int BlockHeight { get; set; }
        public string DestinationAddress { get; set; }

        public static IEnumerable<TransactionOutput> Create(NBitcoin.Transaction transaction, BlockInformation blockInformation, Network network)
        {
            return transaction.Outputs.AsIndexedOutputs().Select(output => Create(output, blockInformation, transaction, network));
        }

        public static TransactionOutput Create(IndexedTxOut output, BlockInformation blockInformation, NBitcoin.Transaction transaction, Network network)
        {
            return new TransactionOutput
            {
                BlockHeight = blockInformation.Height,
                BlockId = blockInformation.BlockId.ToString(),
                BtcSatoshiAmount = output.ToCoin().Amount.Satoshi,
                Index = output.N,
                TransactionId = transaction.GetHash().ToString(),
                DestinationAddress = output.TxOut.ScriptPubKey.GetDestinationAddress(network)?.ToWif()
            };
        }
    }

    #endregion


    #region TransactionInput

    public class TransactionInput : ITransactionInput
    {
        public string TransactionId { get; set; }
        public string BlockId { get; set; }
        public int BlockHeight { get; set; }
        public uint Index { get; set; }
        public IInputTxIn InputTxIn { get; set; }

        public static IEnumerable<TransactionInput> Create(
            NBitcoin.Transaction transaction, 
            BlockInformation blockInformation,
            Network network)
        {
            return transaction.Inputs.AsIndexedInputs()
                .Where(input => !input.PrevOut.IsNull)
                .Select(input => TransactionInput.Create(input, blockInformation, transaction, network));
        }

        public static TransactionInput Create(IndexedTxIn indexedTxIn, BlockInformation blockInformation, NBitcoin.Transaction transaction, Network network)
        {
            
            return new TransactionInput
            {
                BlockHeight = blockInformation.Height,
                BlockId = blockInformation.BlockId.ToString(),
                TransactionId = transaction.GetHash().ToString(),
                Index = indexedTxIn.Index,
                InputTxIn = Block.InputTxIn.Create(indexedTxIn.PrevOut)
            };
        }
    }


    public class InputTxIn : IInputTxIn
    {
        public string TransactionId { get; set; }
        public uint Index { get; set; }

        public static InputTxIn Create(OutPoint outPoint)
        {
            return new InputTxIn
            {
                Index = outPoint.N,
                TransactionId = outPoint.Hash.ToString()
            };
        }
        
    }

    #endregion

    
    public class BlockService: IBlockService
    {
        private readonly ITransactionOutputRepository _outputRepository;
        private readonly ITransactionInputRepository _inputRepository;
        private readonly Network _network;

        public BlockService(ITransactionOutputRepository outputRepository, 
            BaseSettings baseSettings, 
            ITransactionInputRepository inputRepository)
        {
            _outputRepository = outputRepository;
            _inputRepository = inputRepository;
            _network = baseSettings.UsedNetwork();
        }

        public async Task Parse(GetBlockResponse block)
        {
            var inputs = block.Block.Transactions
                .SelectMany(transaction => TransactionInput.Create(transaction, block.AdditionalInformation, _network))
                .ToList();

            var outputs = block.Block.Transactions
                .SelectMany(transaction => TransactionOutput.Create(transaction, block.AdditionalInformation, _network))
                .ToList();

            var insertInputs = _inputRepository.Insert(inputs);
            var insertOutputs =  _outputRepository.Insert(outputs);
            await Task.WhenAll(insertOutputs, insertInputs);
            

            var setSpendedResult =  await _outputRepository.SetSpendedBulk(inputs);
            await _inputRepository.SetSpendedProcessedBulk(setSpendedResult);
        }
    }
}
