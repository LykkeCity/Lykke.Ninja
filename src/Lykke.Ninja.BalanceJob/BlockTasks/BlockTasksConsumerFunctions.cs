using System;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Lykke.JobTriggers.Triggers.Attributes;
using Lykke.Ninja.Core.AlertNotifications;
using Lykke.Ninja.Core.ParseBlockCommand;
using Lykke.Ninja.Core.Queue;
using Lykke.Ninja.Services;

namespace Lykke.Ninja.BalanceJob.BlockTasks
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

        [QueueTrigger(QueueNames.ParseBlockTasks, notify: true, maxPollingIntervalMs: 10 * 1000, maxDequeueCount:0)]
        public async Task ParseBlock(ParseBlockCommandContext context)
        {
            try
            {
                var maxTryCount = 5;
                await Retry.Try(async () => await _parseBlockCommandFacade.ProcessCommand(context, timeOutMinutes: 10), 
                    maxTryCount: maxTryCount, 
                    exceptionFilter: p => p is TimeoutException, logger: _log);
            }
            catch (Exception e)
            {
                await _log.WriteErrorAsync(nameof(BlockTasksConsumerFunctions), nameof(ParseBlock), context.ToJson(), e);
                await _slack.SendError(nameof(ParseBlock), $"Error on {context.ToJson()} {e.Message}");
                throw;
            }
        }
    }
}
