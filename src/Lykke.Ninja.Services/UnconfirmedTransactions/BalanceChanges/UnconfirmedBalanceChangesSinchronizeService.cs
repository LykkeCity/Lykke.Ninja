using System;
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
                AssetQuantity = (foundOutput.ColoredData?.Quantity ?? 0) * (-1),
                BtcSatoshiAmount = foundOutput.BtcSatoshiAmount * (-1),
                TxId = txId,
                Index = foundOutput.Index,
                IsInput = true,
                SpendTxInput = foundOutput.Index,
                SpendTxId = foundOutput.TransactionId
            };
        }

        public static BalanceChange CreateInput(IBalanceChange spendOutput, string txId)
        {
            return new BalanceChange
            {
                Address = spendOutput.Address,
                AssetId = spendOutput.AssetId,
                SpendTxInput = spendOutput.Index,
                SpendTxId = spendOutput.TxId,
                AssetQuantity = spendOutput.AssetQuantity * (-1),
                BtcSatoshiAmount = spendOutput.BtcSatoshiAmount * (-1),
                Index = spendOutput.Index,
                IsInput = true,
                TxId = txId
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

        #region Synchronyze

        public async Task Synchronyze(IBalanceChangesSynchronizePlan synchronizePlan)
        {
            var insertChanges = InsertChanges(synchronizePlan.TxIdsToAdd);
            var removeChanges = RemoveChanges(synchronizePlan.TxIdsToRemove);

            await Task.WhenAll(removeChanges, insertChanges);
            
        }

        #region RemoveChanges

        private async Task RemoveChanges(IEnumerable<string> txIds)
        {
            await _balanceChangesRepository.Remove(txIds);
            await _unconfirmedStatusesRepository.SetRemovedProcessingStatus(txIds, RemoveProcessStatus.Processed);
        }
        
        #endregion

        #region InsertChanges

        private async Task InsertChanges(IEnumerable<string> txIds)
        {
            var notFoundInputs = new List<GroupedTransactionInputs>();
            foreach (var batch in txIds.Batch(100, p => p.ToList()))
            {
                WriteConsole($"{nameof(InsertChanges)} Batch {batch.Count} started");

                var rawTxs = (await _bitcoinRpcClient.GetRawTransactions(batch.Select(uint256.Parse))).ToDictionary(p => p.GetHash().ToString());

                var failedToRetrieveFromBitcoinClient = batch.Where(p => !rawTxs.ContainsKey(p)).ToList();
                var insertChanges =  InsertChangesInner(rawTxs.Values);

                var setFailed = _unconfirmedStatusesRepository.SetInsertStatus(failedToRetrieveFromBitcoinClient, InsertProcessStatus.Failed);

                await Task.WhenAll(insertChanges, setFailed);

                notFoundInputs.AddRange(insertChanges.Result.NotFoundTransactionInputs);

                WriteConsole($"{nameof(InsertChanges)} Batch {batch.Count} done");
            }

            await InsertUnconfirmedInputs(notFoundInputs);
        }

        private async Task<InsertChangesResult> InsertChangesInner(IEnumerable<Transaction> rawTxs)
        {
            var getConfirmedInputs = GetConfirmedInputs(rawTxs);
            var getOutputs = GetOutputs(rawTxs);

            await Task.WhenAll(getConfirmedInputs, getOutputs);

            var balanceChanges = getConfirmedInputs.Result.Add(getOutputs.Result);

            var insertBalanceChanges = _balanceChangesRepository.Upsert(balanceChanges.FoundBalanceChanges);

            var setDoneStatus = _unconfirmedStatusesRepository.SetInsertStatus(balanceChanges.OkTxIds, InsertProcessStatus.Processed);
            var setFailedStatus = _unconfirmedStatusesRepository.SetInsertStatus(balanceChanges.FailedTxIds, InsertProcessStatus.Failed);

            await Task.WhenAll(insertBalanceChanges, setDoneStatus, setFailedStatus);

            return InsertChangesResult.Create(getConfirmedInputs.Result.NotFoundTransactionInputs);
        }

        private async Task<GetConfirmedInputsResult> GetConfirmedInputs(IEnumerable<Transaction> rawTransactions)
        {
            WriteConsole($"{nameof(GetConfirmedInputs)} started");

            var inputDictionary = rawTransactions.SelectMany(p => p.Inputs.Select(txIn => new
            {
                id = TransactionInputOutputIdGenerator.GenerateId(txIn.PrevOut.Hash.ToString(), txIn.PrevOut.N),
                input = txIn
            })).ToDictionary(p => p.id, p => p.input);

            var allInputIds = inputDictionary.Keys;

            var txInputIdDictionary = rawTransactions.SelectMany(
                tx => tx.Inputs.Select(input => new {
                    spentInputId = TransactionInputOutputIdGenerator.GenerateId(input.PrevOut.Hash.ToString(), input.PrevOut.N),
                    txId = tx.GetHash().ToString()})
                ).ToDictionary(p => p.spentInputId, p => p.txId);

            var foundInputs = new Dictionary<string, ITransactionOutput>();
            var counter = 0;
            do
            {
                counter++;
                var notRetrievedIds = allInputIds.Where(p => !foundInputs.ContainsKey(p)).ToList();

                var outputs = await _confirmedOutputRepository.GetByIds(notRetrievedIds);

                foreach (var foundOutput in outputs)
                {
                    foundInputs[foundOutput.Id] = foundOutput;
                }
            } while (foundInputs.Count != allInputIds.Count && counter <= 1);



            var notFoundInputIds = allInputIds.Where(p => !foundInputs.ContainsKey(p)).ToList();

            var failedTxIds = notFoundInputIds.Select(p => txInputIdDictionary[p]);

            var okTxIds = foundInputs.Keys.Select(p => txInputIdDictionary[p]);

            var result =  GetBalanceChangesResult
                .Create(foundInputs.Select(p => BalanceChange.CreateInput(foundOutput:p.Value, txId: txInputIdDictionary[p.Key])), 
                    okTxIds, 
                    failedTxIds);

            WriteConsole($"{nameof(GetConfirmedInputs)} done");

            return GetConfirmedInputsResult.Create(result, 
                GroupedTransactionInputs.Create(notFoundInputIds, 
                    inpId=> txInputIdDictionary[inpId], 
                    inpId => inputDictionary[inpId]));
        }

        private async Task InsertUnconfirmedInputs(IEnumerable<GroupedTransactionInputs> groupedTxInputs)
        {
            var inputTransactions = groupedTxInputs
                .SelectMany(p => p.Inputs.Select(x => new {input = x, txId = p.TransactionId}))
                .ToDictionary(p => p.input.Id, p => p.txId);
            var inputIds = groupedTxInputs.SelectMany(p => p.Inputs.Select(inp => inp.Id)).ToList();
            var foundInputIds = (await _balanceChangesRepository.GetByIds(inputIds)).ToDictionary(p => p.Id);

            var okTxIds  = new List<string>();

            foreach (var tx in groupedTxInputs){
                if (tx.Inputs.All(p => foundInputIds.ContainsKey(p.Id)))
                {
                    okTxIds.Add(tx.TransactionId);
                }
            }
            var insertChanges =_balanceChangesRepository.Upsert(
                foundInputIds.Values.Select(p =>BalanceChange.CreateInput(p, inputTransactions[p.Id])));
            var setOkStatuses = _unconfirmedStatusesRepository.SetInsertStatus(okTxIds, InsertProcessStatus.Processed);

            await Task.WhenAll(insertChanges, setOkStatuses);
        }

        #region GetOutputs

        private async Task<GetBalanceChangesResult> GetOutputs(IEnumerable<Transaction> rawTransactions)
        {
            WriteConsole($"{nameof(GetOutputs)} {rawTransactions.Count()} tx started");

            var uncoloredChanges = rawTransactions.Where(p => !p.HasValidColoredMarker())
                .SelectMany(p => BalanceChange.CreateUncolored(p, _network));
            var getColoredChanges = GetColoredOutputs(rawTransactions.Where(p => p.HasValidColoredMarker()));

            await Task.WhenAll(getColoredChanges);

            var result = getColoredChanges.Result.Add(uncoloredChanges);

            WriteConsole($"{nameof(GetOutputs)} {rawTransactions.Count()} tx done");

            return result;
        }

        private async Task<GetBalanceChangesResult> GetColoredOutputs(IEnumerable<Transaction> rawTransactions)
        {
            WriteConsole($"{nameof(GetColoredOutputs)} {rawTransactions.Count()} tx started");

            var ninjaTransactions = await Retry.Try(async () => await _ninjaTransactionService.Get(rawTransactions.Select(p => p.GetHash()),
                    withRetrySchedule: false), maxTryCount: 5, logger: _log);

            var balanceChanges =  ninjaTransactions.SelectMany(p => BalanceChange.CreateColoredOutput(p, _network));
            
            WriteConsole($"{nameof(GetColoredOutputs)} {rawTransactions.Count()} tx done");

            return GetBalanceChangesResult.Create(balanceChanges);
        }
        
        #endregion

        #region Classes
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

        private class GetConfirmedInputsResult : GetBalanceChangesResult
        {
            //key tx id, values inputs for transactions
            public IEnumerable<GroupedTransactionInputs> NotFoundTransactionInputs { get; set; }

            public static GetConfirmedInputsResult Create(GetBalanceChangesResult getBalanceChangesResult,
                IEnumerable<GroupedTransactionInputs> notFoundTransactionInputs)
            {
                return new GetConfirmedInputsResult
                {
                    OkTxIds = getBalanceChangesResult.OkTxIds,
                    FailedTxIds = getBalanceChangesResult.FailedTxIds,
                    FoundBalanceChanges = getBalanceChangesResult.FoundBalanceChanges,
                    NotFoundTransactionInputs = notFoundTransactionInputs
                };
            }
        }

        private class InsertChangesResult
        {
            //key tx id, values inputs for transactions
            public IEnumerable<GroupedTransactionInputs> NotFoundTransactionInputs { get; set; }

            public static InsertChangesResult Create(IEnumerable<GroupedTransactionInputs> notFoundTransactionInputs)
            {
                return new InsertChangesResult
                {
                    NotFoundTransactionInputs = notFoundTransactionInputs
                };
            }
        }

        private class GroupedTransactionInputs
        {
            public string TransactionId { get; set; }

            public IEnumerable<Input> Inputs { get; set; }

            public static IEnumerable<GroupedTransactionInputs> Create(IEnumerable<string> inputIds,
                Func<string, string> getTxIdFromInputId, Func<string, TxIn> getInputFromInputId)
            {
                var result = inputIds.Select(inputId => new { inputId, txId = getTxIdFromInputId(inputId), input = getInputFromInputId(inputId) }).ToList()
                    .GroupBy(p => p.txId).Select(p => new GroupedTransactionInputs
                    {
                        TransactionId = p.Key,
                        Inputs = p.Select(x=> Input.Create(x.input))
                    });

                return result;
            }

            public class Input
            {
                public string PrevTxId { get; set; }
                public ulong Index { get; set; }

                public string Id { get; set; }

                public static Input Create(TxIn txIn)
                {

                    return new Input
                    {
                        Index = txIn.PrevOut.N,
                        PrevTxId = txIn.PrevOut.Hash.ToString(),
                        Id = BalanceChangeIdGenerator.GenerateId(txIn.PrevOut.Hash.ToString(), txIn.PrevOut.N)
                    };
                }
            }
        }
        #endregion

        #endregion
        
        #endregion

        private void WriteConsole(string message)
        {
            _console.WriteLine($"{nameof(UnconfirmedBalanceChangesSinchronizeService)} {message}");
        }
    }
}
