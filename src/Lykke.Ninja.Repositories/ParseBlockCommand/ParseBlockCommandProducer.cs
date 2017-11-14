using System.Threading.Tasks;
using AzureStorage.Queue;
using Common;
using Common.Extensions;
using Lykke.Ninja.Core.ParseBlockCommand;

namespace Lykke.Ninja.Repositories.ParseBlockCommand
{
    public class ParseBlockCommandProducer: IParseBlockCommandProducer
    {
        private readonly IQueueExt _queue;

        public ParseBlockCommandProducer(IQueueExt queue)
        {
            _queue = queue;
        }
        

        public async Task CreateParseBlockCommand(string blockId, int blockHeight)
        {
            var msg = new ParseBlockCommandContext
            {
                BlockId = blockId,
                BlockHeight = blockHeight
            };

            
            await _queue.PutRawMessageAsync(msg.ToJson());
        }

        public async Task<int> GetQueuedCommandCount()
        {
            return await _queue.Count().WithTimeout(3*60*1000) ?? 0;
        }
    }
}
