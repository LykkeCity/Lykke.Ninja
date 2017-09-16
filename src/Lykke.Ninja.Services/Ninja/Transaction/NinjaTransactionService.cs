using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Ninja.Core.Ninja.Transaction;
using NBitcoin;
using QBitNinja.Client;
using QBitNinja.Client.Models;

namespace Lykke.Ninja.Services.Ninja.Transaction
{
    public class NinjaTransactionService: INinjaTransactionService
    {
        private readonly QBitNinjaClient _ninjaClient;
        private readonly ILog _log;
        private static SemaphoreSlim _lock = new SemaphoreSlim(100);

        public NinjaTransactionService(QBitNinjaClient qBitNinja, ILog log)
        {
            _ninjaClient = qBitNinja;
            _log = log;
        }
        

        public async Task<GetTransactionResponse> Get(uint256 txId, bool withRetry)
        {
            if (withRetry)
            {
                return await Retry.Try(async () => await _ninjaClient.GetTransaction(txId), logger: _log);
            }

            return await _ninjaClient.GetTransaction(txId);
        }

        public async Task<IEnumerable<GetTransactionResponse>> Get(IEnumerable<uint256> txIds, bool withRetry)
        {
            var tasksToAwait = new List<Task>();
            var result = new ConcurrentBag<GetTransactionResponse>();


            foreach ( var txId  in txIds)
            {
                await _lock.WaitAsync();
                var tsk = Get(txId, withRetry)
                    .ContinueWith(p =>
                    {
                        _lock.Release();
                        result.Add(p.Result);
                    });

                tasksToAwait.Add(tsk);

            }

            await Task.WhenAll(tasksToAwait);
            return result;

        }
    }
}
