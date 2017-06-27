using System.Threading.Tasks;
using AzureStorage.Queue;
using Common;
using Core.ParseBlockCommand;

namespace Repositories.ParseBlockCommand
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
            return await _queue.Count() ?? 0;
        }
    }
}
