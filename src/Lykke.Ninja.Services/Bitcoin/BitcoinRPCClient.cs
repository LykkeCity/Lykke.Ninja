using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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

        public BitcoinRpcClient(RPCClient client, IConsole console)
        {
            _client = client;
            _console = console;
        }

        public async Task<IEnumerable<uint256>> GetUnconfirmedTransactionIds()
        {
            return await _client.GetRawMempoolAsync();
        }

        public async Task<IEnumerable<Transaction>> GetRawTransactions(IEnumerable<uint256> txIds)
        {
            var tasksToAwait = new List<Task>();
            var result = new ConcurrentBag<Transaction>();

            var counter = txIds.Count();
            foreach (var txId in txIds)
            {

                await _lock.WaitAsync();
                var tsk = _client.GetRawTransactionAsync(txId, true)
                    .ContinueWith(p =>
                    {
                        counter--;
                        _console.WriteLine(counter.ToString());
                        _lock.Release();
                        if (!p.IsFaulted)
                        {
                            result.Add(p.Result);
                        }
                    });

                tasksToAwait.Add(tsk);

            }

            await Task.WhenAll(tasksToAwait);
            return result;
        }
    }
}
