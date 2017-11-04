using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Common.Extensions;
using Common.Log;
using Lykke.Ninja.Core.Bitcoin;
using NBitcoin;
using NBitcoin.RPC;

namespace Lykke.Ninja.Services.Bitcoin
{
    public class BitcoinRpcClient: IBitcoinRpcClient
    {
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(100);
        private readonly IConsole _console;
        private readonly ILog _log;
        private readonly IBitcoinRpcClientFactory _bitcoinRpcClientFactory;

        public BitcoinRpcClient(IConsole console,
            ILog log, 
            IBitcoinRpcClientFactory bitcoinRpcClientFactory)
        {
            _console = console;
            _log = log;
            _bitcoinRpcClientFactory = bitcoinRpcClientFactory;
        }

        public async Task<IEnumerable<uint256>> GetUnconfirmedTransactionIds()
        {
            return await _bitcoinRpcClientFactory.GetClient().GetRawMempoolAsync();
        }

        public async Task<IEnumerable<Transaction>> GetRawTransactions(IEnumerable<uint256> txIds)
        {
            var tasksToAwait = new List<Task>();
            var result = new ConcurrentBag<Transaction>();
            
            WriteConsole($"Retrieving  txs {txIds.Count()} started");

            foreach (var txId in txIds)
            {
                tasksToAwait.Add(PutRawTransactionInBag(txId, result));
            }
            
            await Task.WhenAll(tasksToAwait).ConfigureAwait(false);

            WriteConsole($"Retrieving txs {result.Count} of {txIds.Count()} done");
            return result;
        }

        private async Task PutRawTransactionInBag(uint256 txId,
            ConcurrentBag<Transaction> transactions)
        {
            var tx = await GetRawTransaction(txId).ConfigureAwait(false);
            if (tx != null)
            {
                transactions.Add(tx);
            }
        }
        
        private async Task<Transaction> GetRawTransaction(uint256 txId)
        {
            await _lock.WaitAsync();
            try
            {
                var result =  await _bitcoinRpcClientFactory.GetClient().GetRawTransactionAsync(txId, false).ConfigureAwait(false);



                return result;
            }
            catch (Exception)
            {

                return null;
            }
            finally
            {
                _lock.Release();
            }
        }

        private void WriteConsole(string message)
        {
            _console.WriteLine($"{nameof(BitcoinRpcClient)} {message}");
        }
    }
}
