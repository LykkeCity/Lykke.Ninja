using System;
using System.Threading.Tasks;
using AzureStorage.Queue;
using Common;
using Lykke.Ninja.Core.UnconfirmedBalances.BalanceChanges;

namespace Lykke.Ninja.Repositories.UnconfirmedBalances
{
    public class UnconfirmedBalanceChangesCommandProducer: IUnconfirmedBalanceChangesCommandProducer
    {
        private readonly Func<IQueueExt> _queue;

        public UnconfirmedBalanceChangesCommandProducer(Func<IQueueExt> queue)
        {
            _queue = queue;
        }

        public async Task ProduceSynchronizeCommand()
        {
            var msg = new BalanceChangeSynchronizeCommandContext();

            await _queue().PutRawMessageAsync(msg.ToJson());
        }

        public async Task<bool> IsQueueFull()
        {
            return await _queue().Count() > 5;
        } 
    }
}
