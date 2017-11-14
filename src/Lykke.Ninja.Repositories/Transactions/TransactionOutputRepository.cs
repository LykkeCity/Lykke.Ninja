using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Ninja.Core.AssetStats;
using Lykke.Ninja.Core.Settings;
using Lykke.Ninja.Core.Transaction;
using Lykke.Ninja.Core.UnconfirmedBalances.BalanceChanges;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using NBitcoin;
using IColoredOutputData = Lykke.Ninja.Core.Transaction.IColoredOutputData;

namespace Lykke.Ninja.Repositories.Transactions
{

    public class SetSpendableOperationResult: ISetSpendableOperationResult
    {
        public IEnumerable<ITransactionInput> Ok { get; set; }

        public IEnumerable<ITransactionInput> NotFound { get; set; }

        public static SetSpendableOperationResult Create(IEnumerable<ITransactionInput> ok,
            IEnumerable<ITransactionInput> notFound)
        {
            return new SetSpendableOperationResult
            {
                NotFound = notFound,
                Ok = ok
            };
        }
    }

    public class AssetStatsAddressSummary : IAssetStatsAddressSummary
    {
        public string Address { get; set; }
        public double Balance { get; set; }

        public static AssetStatsAddressSummary Create(string address, double balance)
        {
            return new AssetStatsAddressSummary
            {
                Address = address,
                Balance = balance
            };
        }
    }

    public class AssetStatsTransaction : IAssetStatsTransaction
    {
        public string Hash { get; set; }

        public static AssetStatsTransaction Create(string hash)
        {
            return new AssetStatsTransaction
            {
                Hash = hash
            };
        }
    }

    public class AssetStatsBlock : IAssetStatsBlock
    {
        public int Height { get; set; }

        public static AssetStatsBlock Create(int height)
        {
            return new AssetStatsBlock
            {
                Height = height
            };
        }
    }

    public class AddressChange : IAddressChange
    {
        public string Address { get; set; }
        public double Quantity { get; set; }

        public static AddressChange Create(string address, double quantity)
        {
            return new AddressChange
            {
                Address = address,
                Quantity = quantity
            };
        }
    }
    
    public class TransactionOutputRepository : ITransactionOutputRepository, IAssetStatsService
    {
        private readonly IMongoCollection<TransactionOutputMongoEntity> _collection;
        private readonly ILog _log;
        private readonly IConsole _console;

        private readonly Lazy<Task> _ensureQueryIndexesLocker;
        private readonly Lazy<Task> _ensureInsertIndexesLocker;
        private readonly Lazy<Task> _ensureUpdateIndexesLocker;

        private readonly AggregateOptions _defaultAggregateOptions;

        public TransactionOutputRepository(ILog log, IConsole console, IMongoCollection<TransactionOutputMongoEntity> collection)
        {
            _log = log;
            _console = console;
            _collection = collection;

            _ensureQueryIndexesLocker = new Lazy<Task>(SetQueryIndexes);
            _ensureInsertIndexesLocker = new Lazy<Task>(SetInsertionIndexes);
            _ensureUpdateIndexesLocker = new Lazy<Task>(SetUpdateIndexes);

            _defaultAggregateOptions = new AggregateOptions { MaxTime = TimeSpan.FromSeconds(35) };
        }

        private void WriteConsole(int blockHeight, string message)
        {
            _console.WriteLine($"{nameof(TransactionOutputRepository)} Block Height:{blockHeight} {message}");
        }

        private void WriteConsole(string message)
        {
            _console.WriteLine($"{nameof(TransactionOutputRepository)}  {message}");
        }

        public async Task InsertIfNotExists(IEnumerable<ITransactionOutput> items)
        {
            await EnsureInsertionIndexes();

            var allIds = items.Select(p => p.Id);

            var existed = await _collection.AsQueryable().Where(p => allIds.Contains(p.Id)).Select(p => p.Id).ToListAsync();

            var itemsToInsert = items.Where(p => !existed.Contains(p.Id)).ToList();

            if (itemsToInsert.Any())
            {
                await _collection.InsertManyAsync(itemsToInsert.Select(TransactionOutputMongoEntity.Create), new InsertManyOptions { IsOrdered = false });
            }
        }

        public async Task InsertIfNotExists(IEnumerable<ITransactionOutput> items, int blockHeight)
        {
            await EnsureInsertionIndexes();

            WriteConsole(blockHeight, "Retrieving existed started");
            var existed = await _collection.AsQueryable().Where(p => p.BlockHeight == blockHeight).Select(p => p.Id).ToListAsync();
            WriteConsole(blockHeight, "Retrieving existed done");

            var itemsToInsert = items.Where(p => !existed.Contains(p.Id)).ToList();
            try
            {
                WriteConsole(blockHeight, "Insert started");
                await Insert(itemsToInsert);
                WriteConsole(blockHeight, "Insert done");
            }
            catch (Exception) // todo catch mongoDuplicate exception
            {
                await InsertIfNotExists(items);
            }
        }

        private async Task Insert(IEnumerable<ITransactionOutput> items)
        {
            await EnsureInsertionIndexes();

            if (items.Any())
            {
                await _collection.InsertManyAsync(items.Select(TransactionOutputMongoEntity.Create), new InsertManyOptions { IsOrdered = false });
            }
        }

        public async Task<ISetSpendableOperationResult> SetSpended(IEnumerable<ITransactionInput> inputs)
        {
            await EnsureUpdateIndexes();

            var spendOutputIds = inputs.Select(
                input => TransactionOutputMongoEntity.GenerateId(input.TxIn.Id));
            
            WriteConsole("Get existed spend outputs started");
            var foundOutputs = await _collection.AsQueryable().Where(p => spendOutputIds.Contains(p.Id)).Select(p => p.Id).ToListAsync();
            WriteConsole("Get existed spend outputs done");

            var inputsDictionary = inputs.ToDictionary(
                p => TransactionOutputMongoEntity.GenerateId(p.TxIn.Id));

            if (foundOutputs.Any())
            {
                var bulkOps = new List<WriteModel<TransactionOutputMongoEntity>>();
                foreach (var id in foundOutputs)
                {
                    var input = inputsDictionary[id];

                    var updateOneOp = new UpdateOneModel<TransactionOutputMongoEntity>(
                        TransactionOutputMongoEntity.Filter.EqId(id),
                        TransactionOutputMongoEntity.Update.SetSpended(input));

                    bulkOps.Add(updateOneOp);
                }

                WriteConsole("Update spended started");
                await _collection.BulkWriteAsync(bulkOps, new BulkWriteOptions{ IsOrdered = false });
                WriteConsole("Update spended done");
            }


            var ok = foundOutputs.Select(id => inputsDictionary[id]);
            var foundOutputsDic = foundOutputs.ToDictionary(p => p); // improve contains efficiency
            var notFoundInputs = spendOutputIds.Where(id => !foundOutputsDic.ContainsKey(id)).Select(id => inputsDictionary[id]).ToList();
            var result = SetSpendableOperationResult.Create(ok, notFoundInputs);

            WriteConsole($"Summary: Ok: {result.Ok.Count()}. NotFound: {result.NotFound.Count()}");

            return result;
        }

        public async Task<long?> GetTransactionsCount(BitcoinAddress address,
            int? at)
        {
            //the ugly way

            int? timeOutMagicValue = null;

            await EnsureQueryIndexes();

            var stringAddress = address.ToString();

            var query = _collection.AsQueryable(new AggregateOptions { MaxTime = TimeSpan.FromSeconds(5) })
                .Where(output => output.DestinationAddress == stringAddress);

            if (at == null)
            {
                try
                {
                 return await query
                        .Select(p => new[] { p.TransactionId, p.SpendTxInput.SpendedInTxId })
                        .SelectMany(p => p)
                        .Where(p => p != null)
                        .Distinct()
                        .CountAsync();
                }
                catch (MongoExecutionTimeoutException)
                {
                    return timeOutMagicValue;
                }
            }

            var inputsQuery = query.Where(p => p.SpendTxInput.BlockHeight <= at);
            var outputsQuery = query.Where(p => p.BlockHeight <= at);

            try
            {
                var inputsTxs = inputsQuery.Select(p => p.SpendTxInput.SpendedInTxId)
                    .Where(p => p != null)
                    .Distinct()
                    .ToListAsync();

                var outputsTxs = outputsQuery.Select(p => p.TransactionId)
                    .Where(p => p != null)
                    .Distinct()
                    .ToListAsync();

                await Task.WhenAll(inputsTxs, outputsTxs);

                return inputsTxs.Result.Union(outputsTxs.Result).Distinct().Count();
            }
            catch (MongoExecutionTimeoutException)
            {
                return timeOutMagicValue;
            }

        }

        public async Task<long?> GetSpendTransactionsCount(BitcoinAddress address,
            int? at)
        {
            long? timeOutMagicValue = null;

            await EnsureQueryIndexes();

            var stringAddress = address.ToString();

            var query = _collection.AsQueryable(new AggregateOptions { MaxTime = TimeSpan.FromSeconds(5) })
                .Where(output => output.DestinationAddress == stringAddress);

            if (at != null)
            {
                query = query.Where(p => p.SpendTxInput.IsSpended)
                    .Where(p => p.SpendTxInput.BlockHeight <= at);
            }
            else
            {
                query = query.Where(p => p.SpendTxInput.IsSpended);
            }
            try
            {
                return await query.Select(p => p.SpendTxInput.SpendedInTxId)
                    .Distinct()
                    .CountAsync();
            }
            catch (MongoExecutionTimeoutException)
            {
                return timeOutMagicValue;
            }
        }

        public async Task<long> GetBtcAmountSummary(BitcoinAddress address, int? at = null, bool isColored = false)
        {
            await EnsureQueryIndexes();

            var stringAddress = address.ToString();
            var query = _collection.AsQueryable(_defaultAggregateOptions)
                .Where(output => output.DestinationAddress == stringAddress);

            if (at != null)
            {
                query = query.Where(p => !p.SpendTxInput.IsSpended || p.SpendTxInput.BlockHeight > at)
                    .Where(p => p.BlockHeight <= at);
            }
            else
            {
                query = query.Where(p => !p.SpendTxInput.IsSpended);

            }

            if (isColored) // exclude btc with colored assets
            {
                query = query.Where(p => !p.ColoredData.HasColoredData);
            }
            
            return await query
                .SumAsync(p => p.BtcSatoshiAmount);
        }

        public async Task<long> GetBtcReceivedSummary(BitcoinAddress address, int? at, bool isColored = false)
        {
            await EnsureQueryIndexes();

            var stringAddress = address.ToString();
            var query = _collection.AsQueryable(_defaultAggregateOptions);
            query = query.Where(output => output.DestinationAddress == stringAddress);

            if (at != null)
            {
                query = query.Where(p => p.BlockHeight <= at);
            }

            if (isColored)
            {
                query = query.Where(p => !p.ColoredData.HasColoredData);
            }

            return await query
                .SumAsync(p => p.BtcSatoshiAmount);
        }

        public async Task<IReadOnlyDictionary<string, long>> GetAssetsReceived(BitcoinAddress address, int? at = null)
        {
            await EnsureQueryIndexes();

            var stringAddress = address.ToString();
            var query = _collection.AsQueryable(_defaultAggregateOptions)
                .Where(output => output.DestinationAddress == stringAddress)
                .Where(p => p.ColoredData.HasColoredData);

            if (at != null)
            {
                query = query.Where(p => p.BlockHeight <= at);
            }

            var result = await query
                .GroupBy(p => p.ColoredData.AssetId)
                .Select(p => new { assetId = p.Key, sum = p.Sum(x => x.ColoredData.Quantity) })
                .ToListAsync();

            return result.ToDictionary(p => p.assetId, p => p.sum);
        }

        public async Task<IReadOnlyDictionary<string, long>> GetAssetsAmount(BitcoinAddress address, int? at = null)
        {
            await EnsureQueryIndexes();

            var stringAddress = address.ToString();

            var query = _collection.AsQueryable(_defaultAggregateOptions)
                .Where(output => output.DestinationAddress == stringAddress)
                .Where(p => p.ColoredData.HasColoredData);

            if (at != null)
            {
                query = query.Where(p => p.BlockHeight <= at)
                    .Where(p => !p.SpendTxInput.IsSpended || p.SpendTxInput.BlockHeight > at);
            }
            else
            {
                query = query.Where(p => !p.SpendTxInput.IsSpended);
            }

            var result = await query
                .GroupBy(p => p.ColoredData.AssetId)
                .Select(p => new { addr = p.Key, sum = p.Sum(x => x.ColoredData.Quantity) })
                .ToListAsync();

            return result.ToDictionary(p => p.addr, p => p.sum);
        }

        public async Task<IEnumerable<ITransactionOutput>> GetByIds(IEnumerable<string> ids, int timeoutSeconds)
        {
            await EnsureQueryIndexes();

            WriteConsole($"{nameof(GetByIds)} retrieving {ids.Count()} outputs started");

            var result = await _collection.AsQueryable(new AggregateOptions() {MaxTime = TimeSpan.FromSeconds(timeoutSeconds) })
                .Where(p => ids.Contains(p.Id))
                .ToListAsync();
            
            WriteConsole($"{nameof(GetByIds)} retrieving  {result.Count} of {ids.Count()} outputs done");

            return result;
        }

        public async Task<IEnumerable<ITransactionOutput>> GetSpended(BitcoinAddress address,
            int? minBlockHeight = null,
            int? maxBlockHeight = null,
            int? itemsToSkip = null,
            int? itemsToTake = null)
        {
            await EnsureQueryIndexes();

            var stringAddress = address.ToString();

            var query = _collection.AsQueryable(_defaultAggregateOptions)
                .Where(output => output.DestinationAddress == stringAddress)
                .OrderByDescending(p => p.SpendTxInput.BlockHeight)
                .Where(p => p.SpendTxInput.IsSpended);

            if (minBlockHeight != null)
            {
                query = query.Where(p => p.SpendTxInput.BlockHeight >= minBlockHeight.Value);
            }

            if (maxBlockHeight != null)
            {
                query = query.Where(p => p.SpendTxInput.BlockHeight <= maxBlockHeight.Value);
            }
            
            

            if (itemsToSkip != null && itemsToSkip != 0)
            {
                query = query.Skip(itemsToSkip.Value);
            }

            if (itemsToTake != null)
            {
                query = query.Take(itemsToTake.Value);
            }


            return await query
                .ToListAsync();
        }

        public async Task<IEnumerable<ITransactionOutput>> GetReceived(BitcoinAddress address,
            bool unspentOnly = false,
            int? minBlockHeight = null,
            int? maxBlockHeight = null,
            int? itemsToSkip = null,
            int? itemsToTake = null)
        {
            await EnsureQueryIndexes();

            var stringAddress = address.ToString();

            var query = (IMongoQueryable<TransactionOutputMongoEntity>)_collection.AsQueryable(_defaultAggregateOptions)
                .Where(output => output.DestinationAddress == stringAddress)
                .OrderByDescending(p => p.BlockHeight);

            
            if (minBlockHeight != null)
            {
                query = query.Where(p => p.BlockHeight >= minBlockHeight.Value);
            }

            if (maxBlockHeight != null)
            {
                query = query.Where(p => p.BlockHeight <= maxBlockHeight.Value);
            }


            if (unspentOnly)
            {
                query = query.Where(p => !p.SpendTxInput.IsSpended);
            }
            

            if (itemsToSkip != null && itemsToSkip != 0)
            {
                query = query.Skip(itemsToSkip.Value);
            }

            if (itemsToTake != null)
            {
                query = query.Take(itemsToTake.Value);
            }

            return (await query
                .ToListAsync())
                .Where(p => p.BtcSatoshiAmount != 0);
        }

        #region Indexes

        private async Task EnsureInsertionIndexes()
        {
            await _ensureInsertIndexesLocker.Value;
        }

        private async Task EnsureQueryIndexes()
        {
            await _ensureQueryIndexesLocker.Value;
        }

        private async Task EnsureUpdateIndexes()
        {
            await _ensureUpdateIndexesLocker.Value;
        }


        private async Task SetInsertionIndexes()
        {
            await _log.WriteInfoAsync(nameof(TransactionOutputRepository), nameof(SetInsertionIndexes), null, "Started");

            var setIndexes = new List<Task>
            {
                SetHeightIndex(),
                SetIdIndex()
            };

            await Task.WhenAll(setIndexes);

            await _log.WriteInfoAsync(nameof(TransactionOutputRepository), nameof(SetInsertionIndexes), null, "Done");
        }

        private async Task SetUpdateIndexes()
        {
            await _log.WriteInfoAsync(nameof(TransactionOutputRepository), nameof(SetUpdateIndexes), null, "Started");


            var setIndexes = new List<Task>
            {
                SetIdIndex()
            };
            
            await Task.WhenAll(setIndexes);

            await _log.WriteInfoAsync(nameof(TransactionOutputRepository), nameof(SetUpdateIndexes), null, "Done");
        }

        private async Task SetQueryIndexes()
        {
            await _log.WriteInfoAsync(nameof(TransactionOutputRepository), nameof(SetQueryIndexes), null, "Started");

            var setIndexes = new[]
            {
                SetSupportHistorySummaryQueryIndex(),
                SetSupportSummaryQueryIndex(),
                SetSupportGetReceivedQueryIndex(),
                SetSupportGetSpendedQueryIndex(),
                SetSupportAssetsStatsBlocksWithChangesQueryIndex(),
                SetSupportAssetsStatsAddressGroupingQueryIndex(),
                SetSupportAssetsStatsTransactionsQueryIndex(),
                SetSupportSpendedBlockHeightIndex()
            };

            await Task.WhenAll(setIndexes);

            await _log.WriteInfoAsync(nameof(TransactionOutputRepository), nameof(SetQueryIndexes), null, "Done");
        }

        #region Single

        private async Task SetHeightIndex()
        {
            var blockHeightIndex = Builders<TransactionOutputMongoEntity>.IndexKeys.Descending(p => p.BlockHeight);
            await _collection.Indexes.CreateOneAsync(blockHeightIndex, new CreateIndexOptions { Background = false });
        }

        private async Task SetIdIndex()
        {
            var idIndex = Builders<TransactionOutputMongoEntity>.IndexKeys.Descending(p => p.Id);
            await _collection.Indexes.CreateOneAsync(idIndex, new CreateIndexOptions { Unique = true });
        }

        #endregion


        private async Task SetSupportSummaryQueryIndex()
        {
            var address = Builders<TransactionOutputMongoEntity>.IndexKeys.Ascending(p => p.DestinationAddress);
            var isSpended = Builders<TransactionOutputMongoEntity>.IndexKeys.Ascending(p => p.SpendTxInput.IsSpended);
            var hasColoredData = Builders<TransactionOutputMongoEntity>.IndexKeys.Descending(p => p.ColoredData.HasColoredData);
            var height = Builders<TransactionOutputMongoEntity>.IndexKeys.Descending(p => p.BlockHeight);
            var btcValue = Builders<TransactionOutputMongoEntity>.IndexKeys.Descending(p => p.BtcSatoshiAmount);

            var definition = Builders<TransactionOutputMongoEntity>.IndexKeys.Combine(address, isSpended, hasColoredData, height, btcValue);
            await _collection.Indexes.CreateOneAsync(definition, new CreateIndexOptions { Background = false, Name = "SupportSummary" });
        }

        private async Task SetSupportHistorySummaryQueryIndex()
        {
            var address = Builders<TransactionOutputMongoEntity>.IndexKeys.Ascending(p => p.DestinationAddress);
            var isSpended = Builders<TransactionOutputMongoEntity>.IndexKeys.Ascending(p => p.SpendTxInput.IsSpended);
            var hasColoredData = Builders<TransactionOutputMongoEntity>.IndexKeys.Descending(p => p.ColoredData.HasColoredData);
            var height = Builders<TransactionOutputMongoEntity>.IndexKeys.Descending(p => p.BlockHeight);
            var spendHeight = Builders<TransactionOutputMongoEntity>.IndexKeys.Descending(p => p.SpendTxInput.BlockHeight);

            var btcValue = Builders<TransactionOutputMongoEntity>.IndexKeys.Descending(p => p.BtcSatoshiAmount);

            var definition = Builders<TransactionOutputMongoEntity>.IndexKeys.Combine(address, isSpended, spendHeight, height, hasColoredData, btcValue);
            await _collection.Indexes.CreateOneAsync(definition, new CreateIndexOptions { Background = false, Name = "SupportSummaryHistory" });
        }

        private async Task SetSupportGetReceivedQueryIndex()
        {
            var address = Builders<TransactionOutputMongoEntity>.IndexKeys.Ascending(p => p.DestinationAddress);
            var height = Builders<TransactionOutputMongoEntity>.IndexKeys.Descending(p => p.BlockHeight);
            var isSpended = Builders<TransactionOutputMongoEntity>.IndexKeys.Ascending(p => p.SpendTxInput.IsSpended);

            var supportGetSpended = Builders<TransactionOutputMongoEntity>.IndexKeys.Combine(address, height, isSpended);
            await _collection.Indexes.CreateOneAsync(supportGetSpended, new CreateIndexOptions { Background = false, Name = "SupportGetReceived" });
        }

        private async Task SetSupportGetSpendedQueryIndex()
        {
            var address = Builders<TransactionOutputMongoEntity>.IndexKeys.Ascending(p => p.DestinationAddress);
            var spentTxInputBlockHeight = Builders<TransactionOutputMongoEntity>.IndexKeys.Descending(p => p.SpendTxInput.BlockHeight);
            var isSpended = Builders<TransactionOutputMongoEntity>.IndexKeys.Ascending(p => p.SpendTxInput.IsSpended);

            var supportGetSpended = Builders<TransactionOutputMongoEntity>.IndexKeys.Combine(address, spentTxInputBlockHeight, isSpended);
            await _collection.Indexes.CreateOneAsync(supportGetSpended, new CreateIndexOptions { Background = false, Name = "SupportGetSpended" });
        }

        private async Task SetSupportAssetsStatsBlocksWithChangesQueryIndex()
        {
            var hasColoredData = Builders<TransactionOutputMongoEntity>.IndexKeys.Descending(p => p.ColoredData.HasColoredData);
            var asset = Builders<TransactionOutputMongoEntity>.IndexKeys.Ascending(p => p.ColoredData.AssetId);
            var blockHeight = Builders<TransactionOutputMongoEntity>.IndexKeys.Descending(p => p.BlockHeight);
            var spentTxInputBlockHeight = Builders<TransactionOutputMongoEntity>.IndexKeys.Descending(p => p.SpendTxInput.BlockHeight);
            var combineIndex = Builders<TransactionOutputMongoEntity>.IndexKeys.Combine(hasColoredData, asset, blockHeight, spentTxInputBlockHeight);
            var indexOpt =
                new CreateIndexOptions<TransactionOutputMongoEntity>
                {
                    Background = true,
                    Name = "AssetsStatsBlocksWithChanges",
                    PartialFilterExpression = Builders<TransactionOutputMongoEntity>.Filter.Eq(p => p.ColoredData.HasColoredData, true)
                };
            await _collection.Indexes.CreateOneAsync(combineIndex, indexOpt);
        }

        private async Task SetSupportAssetsStatsAddressGroupingQueryIndex()
        {
            var hasColoredData = Builders<TransactionOutputMongoEntity>.IndexKeys.Descending(p => p.ColoredData.HasColoredData);
            var asset = Builders<TransactionOutputMongoEntity>.IndexKeys.Ascending(p => p.ColoredData.AssetId);
            var isSpended = Builders<TransactionOutputMongoEntity>.IndexKeys.Descending(p => p.SpendTxInput.IsSpended);
            var address = Builders<TransactionOutputMongoEntity>.IndexKeys.Descending(p => p.DestinationAddress);
            var quantity = Builders<TransactionOutputMongoEntity>.IndexKeys.Descending(p => p.ColoredData.Quantity);
            var combineIndex = Builders<TransactionOutputMongoEntity>.IndexKeys.Combine(hasColoredData, asset, isSpended, address, quantity);

            var indexOpt =
                new CreateIndexOptions<TransactionOutputMongoEntity>
                {
                    Background = true,
                    Name = "SupportAssetStatsAddressGrouping",
                    PartialFilterExpression = Builders<TransactionOutputMongoEntity>.Filter.Eq(p => p.ColoredData.HasColoredData, true)
                };

            await _collection.Indexes.CreateOneAsync(combineIndex, indexOpt);
        }

        private async Task SetSupportAssetsStatsTransactionsQueryIndex()
        {
            var hasColoredData = Builders<TransactionOutputMongoEntity>.IndexKeys.Descending(p => p.ColoredData.HasColoredData);
            var asset = Builders<TransactionOutputMongoEntity>.IndexKeys.Ascending(p => p.ColoredData.AssetId);
            var blockHeight = Builders<TransactionOutputMongoEntity>.IndexKeys.Descending(p => p.BlockHeight);
            var spentTxInputBlockHeight = Builders<TransactionOutputMongoEntity>.IndexKeys.Descending(p => p.SpendTxInput.BlockHeight);

            var txId = Builders<TransactionOutputMongoEntity>.IndexKeys.Descending(p => p.TransactionId);
            var spendedTxId = Builders<TransactionOutputMongoEntity>.IndexKeys.Descending(p => p.SpendTxInput.SpendedInTxId);

            var inputsCombineIndex = Builders<TransactionOutputMongoEntity>.IndexKeys.Combine(hasColoredData, asset, spentTxInputBlockHeight, spendedTxId);
            var outputsCombineIndex = Builders<TransactionOutputMongoEntity>.IndexKeys.Combine(hasColoredData, asset, blockHeight, txId);

            var hasColoredDataFilterExpression =
                Builders<TransactionOutputMongoEntity>.Filter.Eq(p => p.ColoredData.HasColoredData, true);

            await _collection.Indexes.CreateOneAsync(inputsCombineIndex, 
                new CreateIndexOptions<TransactionOutputMongoEntity>
                {
                    Background = true,
                    Name = "SupportAssetStatsTransactionsInputs",
                    PartialFilterExpression = hasColoredDataFilterExpression
                });

            await _collection.Indexes.CreateOneAsync(outputsCombineIndex, 
                new CreateIndexOptions<TransactionOutputMongoEntity>
                {
                    Background = true,
                    Name = "SupportAssetStatsTransactionsOutputs",
                    PartialFilterExpression = hasColoredDataFilterExpression
                });
        }

        private async Task SetSupportSpendedBlockHeightIndex()
        {
            var isSpended = Builders<TransactionOutputMongoEntity>.IndexKeys.Descending(p => p.SpendTxInput.IsSpended);
            var spendedBlockHeight = Builders<TransactionOutputMongoEntity>.IndexKeys.Ascending(p => p.SpendTxInput.BlockHeight);

            var combineIndex = Builders<TransactionOutputMongoEntity>.IndexKeys.Combine(isSpended, spendedBlockHeight);

            var indexOpt =
                new CreateIndexOptions<TransactionOutputMongoEntity>
                {
                    Background = true,
                    Name = "SupportSpendedBlockHeightIndex",
                    PartialFilterExpression = Builders<TransactionOutputMongoEntity>.Filter.Eq(p => p.SpendTxInput.IsSpended, true)
                };

            await _collection.Indexes.CreateOneAsync(combineIndex, indexOpt);
        }


        #endregion

        public async Task<IEnumerable<IAssetStatsAddressSummary>> GetSummaryAsync(IEnumerable<string> assetIds, int? maxBlockHeight)
        {
            await EnsureQueryIndexes();

            var query = _collection.AsQueryable(_defaultAggregateOptions)
                .Where(p => p.ColoredData.HasColoredData)
                .Where(p => assetIds.Contains(p.ColoredData.AssetId));

            if (maxBlockHeight != null)
            {
                query = query.Where(p => p.BlockHeight <= maxBlockHeight)
                    .Where(p => !p.SpendTxInput.IsSpended || p.SpendTxInput.BlockHeight > maxBlockHeight); ;
            }
            else
            {
                query = query.Where(p => !p.SpendTxInput.IsSpended);
            }

            var grouping = await query.GroupBy(p => p.DestinationAddress).Select(p => new
            {
                Addr = p.Key,
                Balance = p.Sum(x => x.ColoredData.Quantity)
            }).ToListAsync();

            var result = new List<IAssetStatsAddressSummary>();

            foreach (var addrBalance in grouping.Where(p=>p.Balance != 0 && p.Addr != null)){      
                result.Add(AssetStatsAddressSummary.Create(addrBalance.Addr, addrBalance.Balance));
            }

            return result.OrderByDescending(p => p.Balance);
        }

        public async Task<IEnumerable<IAssetStatsTransaction>> GetTransactionsForAssetAsync(IEnumerable<string> assetIds, int? minBlockHeight)
        {
            await EnsureQueryIndexes();

            var query = _collection.AsQueryable(_defaultAggregateOptions)
                .Where(p => p.ColoredData.HasColoredData)
                .Where(output => assetIds.Contains(output.ColoredData.AssetId));

            var inputsQuery = query;
            var outputsQuery = query;

            if (minBlockHeight != null)
            {
                inputsQuery = inputsQuery.Where(p => p.SpendTxInput.BlockHeight >= minBlockHeight);
                outputsQuery = outputsQuery.Where(p => p.BlockHeight >= minBlockHeight);
            }

            var inputsTxs = inputsQuery
                .OrderByDescending(p => p.SpendTxInput.BlockHeight)
                .Select(p => p.SpendTxInput.SpendedInTxId)
                .ToListAsync();

            var outputsTxs = outputsQuery
                .OrderByDescending(p => p.BlockHeight)
                .Select(p => p.TransactionId)
                .ToListAsync();

            await Task.WhenAll(inputsTxs, outputsTxs);

            return inputsTxs.Result.Union(outputsTxs.Result)
                .Where(p=> p!= null)
                .Distinct()
                .ToList()
                .Select(AssetStatsTransaction.Create);
        }

        public async Task<IAssetStatsTransaction> GetLatestTxAsync(IEnumerable<string> assetIds)
        {
            await EnsureQueryIndexes();

            var outputsTxIds = await _collection.AsQueryable(_defaultAggregateOptions)
                .Where(p => p.ColoredData.HasColoredData)
                .Where(p => assetIds.Contains(p.ColoredData.AssetId))
                .OrderByDescending(p => p.BlockHeight)
                .Select(p => p.TransactionId)
                .Take(1)
                .ToListAsync();

            if (outputsTxIds.Any())
            {
                return AssetStatsTransaction.Create(outputsTxIds.First());
            }

            return null;
        }

        public async Task<IEnumerable<IAddressChange>> GetAddressQuantityChangesAtBlock(int blockHeight, 
            IEnumerable<string> assetIds)
        {
            await EnsureQueryIndexes();

            var query = _collection.AsQueryable(_defaultAggregateOptions)
                .Where(p => p.ColoredData.HasColoredData)
                .Where(p => assetIds.Contains(p.ColoredData.AssetId));

            var getReceived = query.Where(p => p.BlockHeight == blockHeight)
                .Select(p => new {Address = p.DestinationAddress, p.ColoredData.Quantity})
                .ToListAsync();

            var getSpended = query.Where(p => p.SpendTxInput.BlockHeight == blockHeight)
                .Select(p => new { Address = p.DestinationAddress, p.ColoredData.Quantity})
                .ToListAsync();

            await Task.WhenAll(getReceived, getSpended);

            var result = new Dictionary<string, double>();

            foreach (var receivedChange in getReceived.Result){
                if (result.ContainsKey(receivedChange.Address))
                {
                    result[receivedChange.Address] = result[receivedChange.Address] + receivedChange.Quantity;
                }
                else
                {

                    result[receivedChange.Address] =  receivedChange.Quantity;
                }
            }

            foreach (var spendedChange in getSpended.Result)
            {
                if (result.ContainsKey(spendedChange.Address))
                {
                    result[spendedChange.Address] = result[spendedChange.Address] - spendedChange.Quantity;
                }
                else
                {

                    result[spendedChange.Address] = (-1) * spendedChange.Quantity;
                }
            }

            return result.Select(p => AddressChange.Create(p.Key, p.Value));
        }

        public async Task<IEnumerable<IAssetStatsBlock>> GetBlocksWithChanges(IEnumerable<string> assetIds)
        {
            await EnsureQueryIndexes();

            var data = await _collection.AsQueryable(_defaultAggregateOptions)
                .Where(p => p.ColoredData.HasColoredData)
                .Where(p => assetIds.Contains(p.ColoredData.AssetId))
                .OrderBy(p=>p.BlockHeight)  //force to use AssetsStatsBlocksWithChanges index
                .ThenBy(p=>p.SpendTxInput.BlockHeight) //force to use AssetsStatsBlocksWithChanges index
                .Select(p => new { h = p.BlockHeight, sp = p.SpendTxInput.BlockHeight })
                .ToListAsync();

            return data
                .Select(p=> new []{ p.h, p.sp })
                .SelectMany(p => p)
                .Distinct()
                .Where(p => p != 0)
                .OrderByDescending(p => p)
                .Select(AssetStatsBlock.Create);
        }
    }

    public class TransactionOutputMongoEntity: ITransactionOutput
    {
        public const string CollectionName = "transaction-outputs";

        [BsonId(IdGenerator = typeof(ObjectIdGenerator))]
        public ObjectId _id { get; set; }

        public string Id { get; set; }
        
        public string BlockId { get; set; }

        public int BlockHeight { get; set; }

        public string TransactionId { get; set; }

        public ulong Index { get; set; }

        public long BtcSatoshiAmount { get; set; }

        public string DestinationAddress { get; set; }

        IColoredOutputData ITransactionOutput.ColoredData => ColoredData.HasColoredData? ColoredData:null;

        public TransactionOutputColoredInfoMongoEntity ColoredData { get; set; }

        public static string GenerateId(string id)
        {
            return id;
        }
        
        public SpendTxInputMongoEntity SpendTxInput { get; set; }

        ISpendTxInput ITransactionOutput.SpendTxInput => SpendTxInput;

        public static TransactionOutputMongoEntity Create(ITransactionOutput source)
        {
            return new TransactionOutputMongoEntity
            {
                Id = GenerateId(source.Id),
                BlockHeight = source.BlockHeight,
                BlockId = source.BlockId,
                Index = source.Index,
                TransactionId = source.TransactionId,
                BtcSatoshiAmount = source.BtcSatoshiAmount,
                DestinationAddress = source.DestinationAddress,
                SpendTxInput = SpendTxInputMongoEntity.CreateNotSpended(),
                ColoredData = TransactionOutputColoredInfoMongoEntity.Create(source.ColoredData)
            };
        }

        public static class Filter
        {
            public static FilterDefinition<TransactionOutputMongoEntity> EqId(string id)
            {
                return Builders<TransactionOutputMongoEntity>.Filter.Eq(p => p.Id, id);
            }

            public static FilterDefinition<TransactionOutputMongoEntity> EqAddress(BitcoinAddress address)
            {
                return Builders<TransactionOutputMongoEntity>.Filter.Eq(p => p.DestinationAddress, address.ToString());
            }
        }

        public static class Update
        {
            public static UpdateDefinition<TransactionOutputMongoEntity> SetSpended(ITransactionInput input)
            {
                return Builders<TransactionOutputMongoEntity>.Update.Set(p => p.SpendTxInput,
                    SpendTxInputMongoEntity.CreateSpended(input));
            }
        }
    }

    public class SpendTxInputMongoEntity: ISpendTxInput
    {
        public string Id { get; set; }

        public ulong Index { get; set; }

        public bool IsSpended { get; set; }

        public string SpendedInTxId { get; set; }

        public string BlockId { get; set; }

        public int BlockHeight { get; set; }

        public static SpendTxInputMongoEntity CreateSpended(ITransactionInput input)
        {
            return new SpendTxInputMongoEntity
            {
                IsSpended = true,
                SpendedInTxId = input.TransactionId,
                Id = input.Id,
                Index = input.Index,
                BlockHeight = input.BlockHeight,
                BlockId = input.BlockId
            };
        }

        public static SpendTxInputMongoEntity CreateNotSpended()
        {
            return new SpendTxInputMongoEntity
            {
                IsSpended = false,
                SpendedInTxId = null
            };
        }
    }

    public class TransactionOutputColoredInfoMongoEntity:IColoredOutputData
    {
        public string AssetId { get; set; }
        public long Quantity { get; set; }

        public bool HasColoredData { get; set; }

        public static TransactionOutputColoredInfoMongoEntity Create(IColoredOutputData source)
        {
            if (source != null)
            {
                return new TransactionOutputColoredInfoMongoEntity
                {
                    AssetId = source.AssetId,
                    Quantity = source.Quantity,
                    HasColoredData = true
                };
            }

            return new TransactionOutputColoredInfoMongoEntity
            {
                HasColoredData = false
            };
        }
    }
}
