using System.Threading.Tasks;
using AzureStorage.Queue;
using Common;
using Core.AlertNotifications;
using Lykke.JobTriggers.Abstractions;

namespace Repositories.AlertNotifications
{
    public class SlackNotificationsProducer : ISlackNotificationsProducer, IPoisionQueueNotifier
    {
        private readonly IQueueExt _queueExt;

        public SlackNotificationsProducer(IQueueExt queueExt)
        {
            _queueExt = queueExt;
        }

        public Task SendNotification(string type, string message, string sender)
        {
            return
                _queueExt.PutRawMessageAsync(
                    new SlackNotificationRequestMsg { Message = message, Sender = sender, Type = type }.ToJson());
        }

        public Task NotifyAsync(string message)
        {
            return
                _queueExt.PutRawMessageAsync(
                    new SlackNotificationRequestMsg { Message = message, Sender = "Lykke.Ninja", Type = "PoisonQueueNotifier" }.ToJson());
        }
    }
}
