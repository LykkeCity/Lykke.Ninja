using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Core.Ninja.Transaction;
using NBitcoin;
using QBitNinja.Client;
using QBitNinja.Client.Models;

namespace Services.Ninja.Transaction
{
    public class NinjaTransactionService: INinjaTransactionService
    {
        private readonly QBitNinjaClient _ninjaClient;

        public NinjaTransactionService(QBitNinjaClient qBitNinja)
        {
            _ninjaClient = qBitNinja;
        }
        

        public async Task<GetTransactionResponse> Get(uint256 txId)
        {
            return await Retry.Try(async () => await _ninjaClient.GetTransaction(txId));
        }

        public async Task<IEnumerable<GetTransactionResponse>> Get(IEnumerable<uint256> txIds)
        {
            var tasksToAwait = new List<Task>();
            var result = new List<GetTransactionResponse>();

            foreach ( var txId  in txIds)
            {
                var tsk = Get(txId)
                    .ContinueWith(p =>
                    {
                        result.Add(p.Result);
                    });

                tasksToAwait.Add(tsk);

            }

            await Task.WhenAll(tasksToAwait);
            return result;

        }
    }
}
