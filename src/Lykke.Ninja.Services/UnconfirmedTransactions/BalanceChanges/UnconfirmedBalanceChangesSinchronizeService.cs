using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Ninja.Core.Bitcoin;
using Lykke.Ninja.Core.Ninja.Transaction;
using Lykke.Ninja.Core.Transaction;
using Lykke.Ninja.Core.UnconfirmedBalances.BalanceChanges;
using Lykke.Ninja.Core.UnconfirmedBalances.Statuses;
using Lykke.Ninja.Repositories.Transactions;
using Lykke.Ninja.Services.Block;
using MoreLinq;
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
        public string Id => BalanceChangeIdGenerator.GenerateId(TxId, Index, SpendTxId);
        public string TxId { get; set; }
        public ulong Index { get; set; }
        public bool IsInput { get; set; }
        public long BtcSatoshiAmount { get; set; }
        public string Address { get; set; }
        public string AssetId { get; set; }
        public long AssetQuantity { get; set; }
        public bool HasColoredData => AssetId != null;
        public string SpendTxId { get; set; }
        public ulong? SpendTxInput { get; set; }

        public static IEnumerable<IBalanceChange> CreateUncolored(Transaction transaction, Network network)
        {
            return transaction.Outputs.AsIndexedOutputs().Select(output => CreateUncoloredOutput(output, transaction, network));
        }

        public static BalanceChange CreateUncoloredOutput(IndexedTxOut output, Transaction transaction, Network network)
        {
            return new BalanceChange
            {
                BtcSatoshiAmount = output.ToCoin().Amount.Satoshi,
                Index = output.N,
                TxId = transaction.GetHash().ToString(),
                Address = output.TxOut.ScriptPubKey.GetDestinationAddress(network)?.ToString(),
                IsInput = false,
                SpendTxInput = null,
                SpendTxId = null
            };
        }

        public static IEnumerable<IBalanceChange> CreateColoredOutput(GetTransactionResponse txResp, Network network)
        {
            var uncolored = txResp.Transaction.Outputs.AsIndexedOutputs().Select(output => CreateUncoloredOutput(output, txResp.Transaction, network));

            var coloredData = txResp.ReceivedCoins.OfType<ColoredCoin>()
                .Select(coloredCoin => new
                {
                    AssetId = coloredCoin.AssetId.ToString(network),
                    Quantity = coloredCoin.Amount.Quantity,
                    Id = BalanceChangeIdGenerator.GenerateId(txResp.Transaction.GetHash().ToString(), coloredCoin.Outpoint.N, spendTxId:null)
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

        public static BalanceChange CreateInput(ITransactionOutput foundOutput, string txId)
        {
            return new BalanceChange
            {
                Address = foundOutput.DestinationAddress,
                AssetId = foundOutput.ColoredData?.AssetId,
                AssetQuantity = foundOutput.ColoredData?.Quantity ?? 0,
                BtcSatoshiAmount = foundOutput.BtcSatoshiAmount,
                TxId = txId,
                Index = foundOutput.Index,
                IsInput = true,
                SpendTxInput = foundOutput.Index,
                SpendTxId = foundOutput.TransactionId
            };
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
        private readonly IConsole _console;
        private readonly ILog _log;


        public UnconfirmedBalanceChangesSinchronizeService(IUnconfirmedBalanceChangesRepository balanceChangesRepository, 
            IUnconfirmedStatusesRepository unconfirmedStatusesRepository, 
            IBitcoinRpcClient bitcoinRpcClient, 
            INinjaTransactionService ninjaTransactionService, 
            ITransactionOutputRepository confirmedOutputRepository, 
            Network network, 
            IConsole console, 
            ILog log)
        {
            _balanceChangesRepository = balanceChangesRepository;
            _unconfirmedStatusesRepository = unconfirmedStatusesRepository;
            _bitcoinRpcClient = bitcoinRpcClient;
            _ninjaTransactionService = ninjaTransactionService;
            _confirmedOutputRepository = confirmedOutputRepository;
            _network = network;
            _console = console;
            _log = log;
        }

        public async Task<IBalanceChangesSynchronizePlan> GetBalanceChangesSynchronizePlan()
        {
            var getIdsToInsert = _unconfirmedStatusesRepository.GetNotRemovedTxIds(InsertProcessStatus.Waiting, InsertProcessStatus.Failed);
            var getIdsToRemove = _unconfirmedStatusesRepository.GetRemovedTxIds(RemoveProcessStatus.Waiting, RemoveProcessStatus.Failed);


            await Task.WhenAll(getIdsToInsert, getIdsToRemove);

            return BalanceChangesSynchronizePlan.Create(getIdsToInsert.Result, getIdsToRemove.Result);
        }

        public async Task Synchronyze(IBalanceChangesSynchronizePlan synchronizePlan)
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
            foreach (var batch in txIds.Batch(1000, p => p.ToList()))
            {
                WriteConsole($"{nameof(InsertChanges)} Batch {batch.Count} started");

                var rawTxs = (await _bitcoinRpcClient.GetRawTransactions(batch.Select(uint256.Parse))).ToDictionary(p => p.GetHash().ToString());

                var failedToRetrieveFromBitcoinClient = batch.Where(p => !rawTxs.ContainsKey(p)).ToList();
                var insertChanges =  InsertChangesInner(rawTxs.Values);

                var setFailed = _unconfirmedStatusesRepository.SetInsertStatus(failedToRetrieveFromBitcoinClient, InsertProcessStatus.Failed);

                await Task.WhenAll(insertChanges, setFailed);

                WriteConsole($"{nameof(InsertChanges)} Batch {batch.Count} done");
            }
        }

        private async Task InsertChangesInner(IEnumerable<Transaction> rawTxs)
        {
            var getConfirmedInputs = GetConfirmedInputs(rawTxs);
            var getOutputs = GetOutputs(rawTxs);

            await Task.WhenAll(getConfirmedInputs, getOutputs);

            var balanceChanges = getConfirmedInputs.Result.Add(getOutputs.Result);

            var insertBalanceChanges = _balanceChangesRepository.Upsert(balanceChanges.FoundBalanceChanges);

            var setDoneStatus = _unconfirmedStatusesRepository.SetInsertStatus(balanceChanges.OkTxIds, InsertProcessStatus.Processed);
            var setFailedStatus = _unconfirmedStatusesRepository.SetInsertStatus(balanceChanges.FailedTxIds, InsertProcessStatus.Failed);

            await Task.WhenAll(insertBalanceChanges, setDoneStatus, setFailedStatus);
        }


        private async Task<GetBalanceChangesResult> GetConfirmedInputs(IEnumerable<Transaction> rawTransactions)
        {
            WriteConsole($"{nameof(GetConfirmedInputs)} started");


            var allInputIds = rawTransactions
                .SelectMany(p => p.Inputs.Select(x => TransactionInputOutputIdGenerator.GenerateId(x.PrevOut.Hash.ToString(), x.PrevOut.N)))
                .ToList();

            var inputsTransactions = rawTransactions.SelectMany(
                tx => tx.Inputs.Select(input => new {
                    spentInputId = TransactionInputOutputIdGenerator.GenerateId(input.PrevOut.Hash.ToString(), input.PrevOut.N),
                    txId = tx.GetHash().ToString()})
                ).ToDictionary(p => p.spentInputId, p => p.txId);

            var foundInputs = new Dictionary<string, ITransactionOutput>();

            var notRetrievedIds = allInputIds.Where(p => !foundInputs.ContainsKey(p)).ToList();

            var outputs = await _confirmedOutputRepository.GetByIds(notRetrievedIds);

            foreach (var foundOutput in outputs)
            {
                foundInputs[foundOutput.Id] = foundOutput;
            }

            var notFoundInputIds = allInputIds.Where(p => !foundInputs.ContainsKey(p)).ToList();

            var failedTxIds = notFoundInputIds.Select(p => inputsTransactions[p]);

            var okTxIds = foundInputs.Keys.Select(p => inputsTransactions[p]);

            var result =  GetBalanceChangesResult
                .Create(foundInputs.Select(p => BalanceChange.CreateInput(foundOutput:p.Value, txId: inputsTransactions[p.Key])), 
                    okTxIds, 
                    failedTxIds);

            WriteConsole($"{nameof(GetConfirmedInputs)} done");

            return result;
        }


        private async Task<GetBalanceChangesResult> GetOutputs(IEnumerable<Transaction> rawTransactions)
        {
            WriteConsole($"{nameof(GetOutputs)} {rawTransactions.Count()} tx started");

            var uncoloredChanges = rawTransactions.Where(p => !p.HasValidColoredMarker())
                .SelectMany(p => BalanceChange.CreateUncolored(p, _network));
            var getColoredChanges = GetColoredChanges(rawTransactions.Where(p => p.HasValidColoredMarker()));

            await Task.WhenAll(getColoredChanges);

            var result = getColoredChanges.Result.Add(uncoloredChanges);

            WriteConsole($"{nameof(GetOutputs)} {rawTransactions.Count()} tx done");

            return result;
        }

        private async Task<GetBalanceChangesResult> GetColoredChanges(IEnumerable<Transaction> rawTransactions)
        {
            WriteConsole($"{nameof(GetColoredChanges)} {rawTransactions.Count()} tx started");

            var ninjaTransactions = await Retry.Try(async () => await _ninjaTransactionService.Get(rawTransactions.Select(p => p.GetHash()),
                    withRetrySchedule: false), maxTryCount: 5, logger: _log);

            var balanceChanges =  ninjaTransactions.SelectMany(p => BalanceChange.CreateColoredOutput(p, _network));
            
            WriteConsole($"{nameof(GetColoredChanges)} {rawTransactions.Count()} tx done");

            return GetBalanceChangesResult.Create(balanceChanges);
        }

        private void WriteConsole(string message)
        {
            _console.WriteLine($"{nameof(UnconfirmedBalanceChangesSinchronizeService)} {message}");
        }

        private class GetBalanceChangesResult
        {
            public IEnumerable<string> OkTxIds { get; set; }

            public IEnumerable<string> FailedTxIds { get; set; }

            public IEnumerable<IBalanceChange> FoundBalanceChanges { get; set; }


            public GetBalanceChangesResult Add(GetBalanceChangesResult added)
            {
                return GetBalanceChangesResult.Create(FoundBalanceChanges.Union(added.FoundBalanceChanges).ToList(),
                    OkTxIds.Union(added.OkTxIds).Distinct().ToList(), 
                    FailedTxIds.Union(added.FailedTxIds).Distinct().ToList());
            }
            public GetBalanceChangesResult Add(IEnumerable<IBalanceChange> added)
            {
                return GetBalanceChangesResult.Create(FoundBalanceChanges.Union(FoundBalanceChanges.Union(added).ToList()),
                    OkTxIds.Union(added.Select(p => p.TxId)).Distinct().ToList(),
                    FailedTxIds = FailedTxIds.ToList());
            }
            public static GetBalanceChangesResult Create(IEnumerable<IBalanceChange> foundBalanceChanges)
            {
                return GetBalanceChangesResult.Create(foundBalanceChanges,
                    foundBalanceChanges.Select(p => p.TxId),
                    null);
            }

            public static GetBalanceChangesResult Create(IEnumerable<IBalanceChange> foundBalanceChanges,
                IEnumerable<string> okTxIds, 
                IEnumerable<string> failedTxIds)
            {
                var failedTxIdDict = (failedTxIds ?? Enumerable.Empty<string>()).Distinct().ToDictionary(p => p);
                return new GetBalanceChangesResult
                {
                    FoundBalanceChanges = foundBalanceChanges,
                    FailedTxIds = failedTxIdDict.Values,
                    OkTxIds = (okTxIds ?? Enumerable.Empty<string>()).Where(p => !failedTxIdDict.ContainsKey(p))
                };
            }
        }
    }
}
