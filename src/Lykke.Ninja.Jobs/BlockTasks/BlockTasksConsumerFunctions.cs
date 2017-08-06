using System;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Lykke.Ninja.Core.AlertNotifications;
using Lykke.Ninja.Core.ParseBlockCommand;
using Lykke.Ninja.Core.Queue;
using Lykke.JobTriggers.Triggers.Attributes;

namespace Lykke.Ninja.Jobs.BlockTasks
{
    public class BlockTasksConsumerFunctions
    {
        private readonly ILog _log;
        private readonly ISlackNotificationsProducer _slack;
        private readonly IProcessParseBlockCommandFacade _parseBlockCommandFacade;

        public BlockTasksConsumerFunctions(ILog log, 
            ISlackNotificationsProducer slack,
            IProcessParseBlockCommandFacade parseBlockCommandFacade)
        {
            _log = log;
            _slack = slack;
            _parseBlockCommandFacade = parseBlockCommandFacade;
        }

        [QueueTrigger(QueueNames.ParseBlockTasks, notify: true, maxPollingIntervalMs: 10 * 1000)]
        public async Task ParseBlock(ParseBlockCommandContext context)
        {
            try
            {
                await _parseBlockCommandFacade.ProcessCommand(context);
            }
            catch (Exception e)
            {
                await _log.WriteErrorAsync(nameof(BlockTasksConsumerFunctions), nameof(ParseBlock), context.ToJson(), e);
                await _slack.SendNotification(nameof(BlockTasksConsumerFunctions), nameof(ParseBlock), $"Error on {context.ToJson()}. Admin attention required");
                throw;
            }
        }
    }
}
