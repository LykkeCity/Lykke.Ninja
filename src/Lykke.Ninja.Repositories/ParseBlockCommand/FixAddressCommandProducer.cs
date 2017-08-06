using System;
using System.Threading.Tasks;
using AzureStorage.Queue;
using Common;
using Lykke.Ninja.Core.ParseBlockCommand;
using NBitcoin;

namespace Lykke.Ninja.Repositories.ParseBlockCommand
{
    public class FixAddressCommandProducer: IFixAddressCommandProducer
    {
        private readonly IQueueExt _queue;

        public FixAddressCommandProducer(IQueueExt queue)
        {
            _queue = queue;
        }

        public async Task CreateFixAddressCommand(BitcoinAddress address)
        {
            var msg = new FixAddressCommandContext
            {
                Address = address.ToWif()
            };


            await _queue.PutRawMessageAsync(msg.ToJson());
        }
    }
}
