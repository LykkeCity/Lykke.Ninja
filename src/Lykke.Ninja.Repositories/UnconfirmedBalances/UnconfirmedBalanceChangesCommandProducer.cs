using System.Threading.Tasks;
using AzureStorage.Queue;
using Common;
using Lykke.Ninja.Core.UnconfirmedBalances.BalanceChanges;

namespace Lykke.Ninja.Repositories.UnconfirmedBalances
{
    public class UnconfirmedBalanceChangesCommandProducer: IUnconfirmedBalanceChangesCommandProducer
    {
        private readonly IQueueExt _queue;

        public UnconfirmedBalanceChangesCommandProducer(IQueueExt queue)
        {
            _queue = queue;
        }

        public async Task ProduceSynchronizeCommand()
        {
            var msg = new BalanceChangeSynchronizeCommandContext();

            //do not create large queue
            if (await _queue.Count() <= 5)
            {
                await _queue.PutRawMessageAsync(msg.ToJson());
            }
        }
    }
}
