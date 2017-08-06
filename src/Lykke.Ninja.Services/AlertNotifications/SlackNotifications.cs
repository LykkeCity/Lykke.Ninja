using System.Threading.Tasks;
using AzureStorage.Queue;
using Common;
using Lykke.Ninja.Core.AlertNotifications;
using Lykke.JobTriggers.Abstractions;
using Lykke.SlackNotifications;

namespace Lykke.Ninja.Services.AlertNotifications
{
    public class SlackNotificationsProducer : ISlackNotificationsProducer, IPoisionQueueNotifier
    {
        private readonly ISlackNotificationsSender _slackClient;

        public SlackNotificationsProducer(ISlackNotificationsSender slackClient)
        {
            _slackClient = slackClient;
        }

        public async Task SendNotification(string type, string message, string sender)
        {

            await _slackClient.SendAsync(type, sender, message);
        }

        public Task NotifyAsync(string message)
        {
            return SendNotification("PoisonQueueNotifier", message, "Lykke.Ninja");
        }
    }
}
