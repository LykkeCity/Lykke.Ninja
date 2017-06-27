﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Core.AlertNotifications;
using Core.Block;
using Core.Settings;
using Core.Transaction;
using NBitcoin;
using QBitNinja.Client.Models;
using Services.Settings;

namespace Services.Block
{
    #region ids

    public static class TransactionInputOutputIdGenerator
    {
        public static string GenerateId(string transactionId, uint index)
        {
            return $"{transactionId}_{index}";
        }
    }

    #endregion

    #region  TransactionOutput

    public class TransactionOutput : ITransactionOutput
    {
        public string Id => TransactionInputOutputIdGenerator.GenerateId(TransactionId, Index);

        public string TransactionId { get; set; }
        public uint Index { get; set; }
        public string OutputHash { get; set; }
        public long BtcSatoshiAmount { get; set; }
        public string BlockId { get; set; }
        public int BlockHeight { get; set; }
        public string DestinationAddress { get; set; }
        public IColoredOutputData ColoredData { get; set; }

        public static IEnumerable<TransactionOutput> Create(Transaction transaction, BlockInformation blockInformation, Network network)
        {
            return transaction.Outputs.AsIndexedOutputs().Select(output => Create(output, blockInformation, transaction, network));
        }

        public static TransactionOutput Create(IndexedTxOut output, BlockInformation blockInformation, Transaction transaction, Network network)
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

        public static void SetColoredToOutputs(IEnumerable<TransactionOutput> transactionOutputs,
            IEnumerable<ColoredOutputData> coloredDatas)
        {
            var outputsDictionary = transactionOutputs.ToDictionary(p => p.Id);

            foreach (var coloredOutputData in coloredDatas)
            {
                outputsDictionary[coloredOutputData.Id].ColoredData = coloredOutputData;
            }
        }
    }
    
    public class ColoredOutputData: IColoredOutputData
    {
        public string Id => TransactionInputOutputIdGenerator.GenerateId(TransactionId, Index);
        public string AssetId { get; set; }
        

        public long Quantity { get; set; }

        public string TransactionId { get; set; }

        public uint Index { get; set; }

        public static IEnumerable<ColoredOutputData> Create(GetTransactionResponse transactionResponse, Network network)
        {
            return transactionResponse.ReceivedCoins.OfType<ColoredCoin>()
                .Select(coloredCoin => Create(coloredCoin, transactionResponse.Transaction, network));
        }

        public static ColoredOutputData Create(ColoredCoin coloredCoin, Transaction transaction, Network network)
        {
            return new ColoredOutputData
            {
                AssetId = coloredCoin.AssetId.ToString(network),
                Quantity = coloredCoin.Amount.Quantity,
                TransactionId = transaction.GetHash().ToString(),
                Index = coloredCoin.Outpoint.N
            };
        }
    }

    #endregion

    #region TransactionInput

    public class TransactionInput : ITransactionInput
    {
        public string Id => TransactionInputOutputIdGenerator.GenerateId(TransactionId, Index);


        public string TransactionId { get; set; }
        public string BlockId { get; set; }
        public int BlockHeight { get; set; }
        public uint Index { get; set; }
        public IInputTxIn TxIn { get; set; }

        public static IEnumerable<TransactionInput> Create(
            Transaction transaction, 
            BlockInformation blockInformation)
        {
            return transaction.Inputs.AsIndexedInputs()
                .Where(input => !input.PrevOut.IsNull)
                .Select(input => Create(input, blockInformation, transaction));
        }

        private static TransactionInput Create(IndexedTxIn indexedTxIn, 
            BlockInformation blockInformation, 
            Transaction transaction)
        {
            
            return new TransactionInput
            {
                BlockHeight = blockInformation.Height,
                BlockId = blockInformation.BlockId.ToString(),
                TransactionId = transaction.GetHash().ToString(),
                Index = indexedTxIn.Index,
                TxIn = InputTxIn.Create(indexedTxIn.PrevOut)
            };
        }
    }


    public class InputTxIn : IInputTxIn
    {
        public string Id => TransactionInputOutputIdGenerator.GenerateId(TransactionId, Index);
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
        private readonly ILog _log;
        private readonly ISlackNotificationsProducer _notificationsProducer;

        public BlockService(ITransactionOutputRepository outputRepository, 
            BaseSettings baseSettings, 
            ITransactionInputRepository inputRepository, 
            ILog log, 
            ISlackNotificationsProducer notificationsProducer)
        {
            _outputRepository = outputRepository;
            _inputRepository = inputRepository;
            _log = log;
            _notificationsProducer = notificationsProducer;
            _network = baseSettings.UsedNetwork();
        }

        public async Task Parse(GetBlockResponse block, 
            IEnumerable<GetTransactionResponse> coloredTransactions)
        {
            var inputs = block.Block.Transactions
                .SelectMany(transaction => TransactionInput.Create(transaction, block.AdditionalInformation))
                .ToList();

            var outputs = block.Block.Transactions
                .SelectMany(transaction => TransactionOutput.Create(transaction, block.AdditionalInformation, _network))
                .ToList();

            var coloredData = coloredTransactions.SelectMany(tx => ColoredOutputData.Create(tx, _network));

            TransactionOutput.SetColoredToOutputs(outputs, coloredData);

            var insertInputs = _inputRepository.InsertIfNotExists(inputs);
            var insertOutputs =  _outputRepository.InsertIfNotExists(outputs);
            await Task.WhenAll(insertOutputs, insertInputs);

            await ProcessInputsToSpendable(inputs);
        }

        public async Task ProcessInputsToSpendable(IEnumerable<ITransactionInput> inputs)
        {
            var setSpendedResult = await _outputRepository.SetSpended(inputs);
            await _inputRepository.SetSpended(setSpendedResult);

            if (setSpendedResult.NotFound.Any())
            {
                var warnMessage = "Failed to set spended outputs " +
                                  $"Failed inputs count {setSpendedResult.NotFound.Count()}";

                await _notificationsProducer.SendNotification(nameof(BlockService),
                    warnMessage,
                    nameof(Parse));

                await _log.WriteWarningAsync(nameof(BlockService),
                    nameof(Parse),
                    new
                    {
                        notFoundIds = setSpendedResult.NotFound.Take(5).ToArray()
                    }.ToJson(),
                    warnMessage);
            }
        }
    }
}