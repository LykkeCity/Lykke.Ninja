using System;
using System.Threading.Tasks;
using AzureStorage.Queue;
using Common;
using Lykke.Ninja.Core.ParseBlockCommand;

namespace Lykke.Ninja.Repositories.ParseBlockCommand
{
    public class ScanNotFoundsCommandProducer:IScanNotFoundsCommandProducer
    {
        private readonly IQueueExt _queue;

        public ScanNotFoundsCommandProducer(IQueueExt queue)
        {
            _queue = queue;
        }

        public async Task CreateScanNotFoundsCommand()
        {
            var msg = new ScanNotFoundsContext();
            
            await _queue.PutRawMessageAsync(msg.ToJson());
        }
    }
}
