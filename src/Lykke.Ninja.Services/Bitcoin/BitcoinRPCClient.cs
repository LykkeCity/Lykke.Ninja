using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Common.Extensions;
using Common.Log;
using Lykke.Ninja.Core.Bitcoin;
using NBitcoin;
using NBitcoin.RPC;
using QBitNinja.Client.Models;

namespace Lykke.Ninja.Services.Bitcoin
{
    public class BitcoinRpcClient: IBitcoinRpcClient
    {
        private readonly RPCClient _client;
        private static SemaphoreSlim _lock = new SemaphoreSlim(100);
        private readonly IConsole _console;
        private readonly ILog _log;


        public BitcoinRpcClient(RPCClient client, IConsole console, ILog log)
        {
            _client = client;
            _console = console;
            _log = log;
        }

        public async Task<IEnumerable<uint256>> GetUnconfirmedTransactionIds(int timeoutSeconds = 10)
        {
            return await _client.GetRawMempoolAsync().WithTimeout(timeoutSeconds * 1000);
        }

        public async Task<IEnumerable<Transaction>> GetRawTransactions(IEnumerable<uint256> txIds, int timeoutSeconds = 10)
        {
            var tasksToAwait = new List<Task>();
            var result = new ConcurrentBag<Transaction>();

            //var counter = txIds.Count();
            //WriteConsole($"Retrieving {counter} txs started");

            foreach (var txId in txIds)
            {
                //WriteConsole($"Awaiting lock {_lock.CurrentCount}");
                await _lock.WaitAsync();
                var tsk = GetRawTransaction(txId, timeoutSeconds)
                    .ContinueWith(p =>
                    {
                        //counter--;
                        //WriteConsole($"Releasing lock {_lock.CurrentCount}");

 

                        try
                        {
                            if (p.Result != null)
                            {
                                result.Add(p.Result);
                            }
                            WriteConsole($"Retrieving {txId.ToString()} done.");
                        }
                        catch (Exception)
                        {
                            WriteConsole($"Error while retrieving {txId} from Bitcoin RPC");
                        }
                        finally
                        {
                            _lock.Release();
                        }
                    });

                tasksToAwait.Add(tsk);

            }

            WriteConsole($"Retrieving txs done");

            await Task.WhenAll(tasksToAwait);

            return result;
        }
        
        private Task<Transaction> GetRawTransaction(uint256 txId, int timeoutSeconds)
        {
            return _client.GetRawTransactionAsync(txId, false).WithTimeout(timeoutSeconds * 1000);
        }

        private void WriteConsole(string message)
        {
            _console.WriteLine($"{nameof(BitcoinRpcClient)} {message}");
        }
    }
}
