using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Ninja.Core.Bitcoin;
using Lykke.Ninja.Core.Ninja.Transaction;
using Lykke.Ninja.Core.Transaction;
using Lykke.Ninja.Core.UnconfirmedBalances.BalanceChanges;
using Lykke.Ninja.Core.UnconfirmedBalances.Statuses;
using Lykke.Ninja.Services.Block;
using NBitcoin;
using NBitcoin.OpenAsset;
using QBitNinja.Client.Models;

namespace Lykke.Ninja.Services.UnconfirmedTransactions.BalanceChanges
{
    public class BalanceChangesSynchronizePlan : IBalanceChangesSynchronizePlan
    {
        public IEnumerable<string> TxIdsToAdd { get; set; }
        public IEnumerable<string> TxIdsToRemove { get; set; }

        public static BalanceChangesSynchronizePlan Create(IEnumerable<string> txIdsToAdd, IEnumerable<string> txIdsToRemove)
        {
            return new BalanceChangesSynchronizePlan
            {
                TxIdsToAdd = txIdsToAdd,
                TxIdsToRemove = txIdsToRemove
            };
        }
    }

    public class BalanceChange : IBalanceChange
    {
        public string Id => BalanceChangeIdGenerator.GenerateId(TxId, Index);
        public string TxId { get; set; }
        public ulong Index { get; set; }
        public bool IsInput { get; set; }
        public long BtcSatoshiAmount { get; set; }
        public string Address { get; set; }
        public string AssetId { get; set; }
        public long AssetQuantity { get; set; }

        public static IEnumerable<IBalanceChange> CreateUncolored(Transaction transaction, Network network)
        {
            return transaction.Outputs.AsIndexedOutputs().Select(output => CreateUncolored(output, transaction, network));
        }

        public static BalanceChange CreateUncolored(IndexedTxOut output, Transaction transaction, Network network)
        {
            return new BalanceChange
            {
                BtcSatoshiAmount = output.ToCoin().Amount.Satoshi,
                Index = output.N,
                TxId = transaction.GetHash().ToString(),
                Address = output.TxOut.ScriptPubKey.GetDestinationAddress(network)?.ToString()
            };
        }

        public static IEnumerable<IBalanceChange> CreateColored(GetTransactionResponse txResp, Network network)
        {
            var uncolored = txResp.Transaction.Outputs.AsIndexedOutputs().Select(output => CreateUncolored(output, txResp.Transaction, network));

            var coloredData = txResp.ReceivedCoins.OfType<ColoredCoin>()
                .Select(coloredCoin => new
                {
                    AssetId = coloredCoin.AssetId.ToString(network),
                    Quantity = coloredCoin.Amount.Quantity,
                    Id = BalanceChangeIdGenerator.GenerateId(txResp.Transaction.GetHash().ToString(), coloredCoin.Outpoint.N)
                });
            

            var balanceChangesDictionary = uncolored.ToDictionary(p => p.Id);

            foreach (var coloredOutputData in coloredData)
            {
                var balanceChange = balanceChangesDictionary[coloredOutputData.Id];

                balanceChange.AssetId = coloredOutputData.AssetId;
                balanceChange.AssetQuantity = coloredOutputData.Quantity;
            }

            return balanceChangesDictionary.Values;
        }
    }

    public class UnconfirmedBalanceChangesSinchronizeService: IUnconfirmedBalanceChangesSinchronizeService
    {
        private readonly IUnconfirmedStatusesRepository _unconfirmedStatusesRepository;
        private readonly IUnconfirmedBalanceChangesRepository _balanceChangesRepository;
        private readonly IBitcoinRpcClient _bitcoinRpcClient;
        private readonly INinjaTransactionService _ninjaTransactionService;
        private readonly ITransactionOutputRepository _confirmedOutputRepository;
        private readonly Network _network;


        public UnconfirmedBalanceChangesSinchronizeService(IUnconfirmedBalanceChangesRepository balanceChangesRepository, 
            IUnconfirmedStatusesRepository unconfirmedStatusesRepository, 
            IBitcoinRpcClient bitcoinRpcClient, 
            INinjaTransactionService ninjaTransactionService, 
            ITransactionOutputRepository confirmedOutputRepository, 
            Network network)
        {
            _balanceChangesRepository = balanceChangesRepository;
            _unconfirmedStatusesRepository = unconfirmedStatusesRepository;
            _bitcoinRpcClient = bitcoinRpcClient;
            _ninjaTransactionService = ninjaTransactionService;
            _confirmedOutputRepository = confirmedOutputRepository;
            _network = network;
        }

        public async Task<IBalanceChangesSynchronizePlan> GetBalanceChangesSynchronizePlan()
        {
            var getIdsToInsert = _unconfirmedStatusesRepository.GetTxIds(InsertProcessStatus.Waiting, InsertProcessStatus.Failed);
            var getIdsToRemove = _unconfirmedStatusesRepository.GetTxIds(RemoveProcessStatus.Waiting, RemoveProcessStatus.Failed);


            await Task.WhenAll(getIdsToInsert, getIdsToRemove);

            return BalanceChangesSynchronizePlan.Create(getIdsToInsert.Result, getIdsToRemove.Result);
        }

        public async Task Synchronize(IBalanceChangesSynchronizePlan synchronizePlan)
        {
            var insertChanges = InsertChanges(synchronizePlan.TxIdsToAdd);
            var removeChanges = RemoveChanges(synchronizePlan.TxIdsToRemove);

            await Task.WhenAll(removeChanges, insertChanges);}

        private async Task RemoveChanges(IEnumerable<string> txIds)
        {
            await _balanceChangesRepository.Remove(txIds);
            await _unconfirmedStatusesRepository.SetRemovedProcessingStatus(txIds, RemoveProcessStatus.Processed);
        }

        private async Task InsertChanges(IEnumerable<string> txIds)
        {
            //debug
           // txIds = txIds.Take(100);

            var rawTxs = (await _bitcoinRpcClient.GetRawTransactions(txIds.Select(uint256.Parse))).ToList();

            var getInputs = GetInputs(rawTxs);
            var getOutputs = GetOutputs(rawTxs);

            await Task.WhenAll(getInputs, getOutputs);


            var balanceChanges = getInputs.Result.Union(getOutputs.Result).ToList();

            var insertBalanceChanges = _balanceChangesRepository.Upsert(balanceChanges);

            var rawTxsDic = rawTxs.ToDictionary(p => p.GetHash().ToString(), p => p);
            var notFoundInBitcoinRpcTxs = txIds.Where(p => !rawTxsDic.ContainsKey(p));
            var foundTxIds = balanceChanges.Select(p => p.TxId).Distinct();

            var setFailedStatus = _unconfirmedStatusesRepository.SetInsertStatus(notFoundInBitcoinRpcTxs, InsertProcessStatus.Failed);
            var setDoneStatus = _unconfirmedStatusesRepository.SetInsertStatus(foundTxIds, InsertProcessStatus.Processed);

            await Task.WhenAll(insertBalanceChanges, setDoneStatus, setFailedStatus);
        }


        private async Task<IEnumerable<IBalanceChange>> GetInputs(IEnumerable<Transaction> rawTransactions)
        {
            var uncolored = rawTransactions.Where(p => !p.HasValidColoredMarker()).ToList();

            var spendOutputIds = uncolored
                .SelectMany(p => p.Inputs
                    .Select(x => TransactionInputOutputIdGenerator.GenerateId(x.PrevOut.Hash.ToString(), x.PrevOut.N)));

            return await _confirmedOutputRepository.GetBalanceChanges(spendOutputIds);
        }


        private async Task<IEnumerable<IBalanceChange>> GetOutputs(IEnumerable<Transaction> rawTransactions)
        {
            var uncoloredChanges = rawTransactions.Where(p => !p.HasValidColoredMarker())
                .SelectMany(p => BalanceChange.CreateUncolored(p, _network));
            var getColoredChanges = GetColoredChanges(rawTransactions);

            await Task.WhenAll(getColoredChanges);

            return uncoloredChanges.Union(getColoredChanges.Result);
        }

        private async Task<IEnumerable<IBalanceChange>> GetColoredChanges(IEnumerable<Transaction> rawTransactions)
        {

            var colored = rawTransactions.Where(p => p.HasValidColoredMarker()).ToList();
            var ninjaTransactions =  await _ninjaTransactionService.Get(colored.Select(p => p.GetHash()), withRetry: false);

            return ninjaTransactions.SelectMany(p => BalanceChange.CreateColored(p, _network));
        }
    }
}
