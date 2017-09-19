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
        private readonly RPCClient _client;
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(100);
        private readonly IConsole _console;
        private readonly ILog _log;


        public BitcoinRpcClient(RPCClient client, IConsole console, ILog log)
        {
            _client = client;
            _console = console;
            _log = log;
        }

        public async Task<IEnumerable<uint256>> GetUnconfirmedTransactionIds()
        {
            return await _client.GetRawMempoolAsync();
        }

        public async Task<IEnumerable<Transaction>> GetRawTransactions(IEnumerable<uint256> txIds)
        {
            var tasksToAwait = new List<Task>();
            var result = new ConcurrentBag<Transaction>();
            
            WriteConsole($"Retrieving  txs started");

            foreach (var txId in txIds)
            {
                tasksToAwait.Add(PutRawTransactionInBag(txId, result));
            }

            
            await Task.WhenAll(tasksToAwait).ConfigureAwait(false);

            WriteConsole($"Retrieving txs done");
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
                var result =  await _client.GetRawTransactionAsync(txId, false).ConfigureAwait(false);

                WriteConsole($"{txId} Retrieving done.");

                return result;
            }
            catch (Exception)
            {

                WriteConsole($"{txId} Not found.");
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
