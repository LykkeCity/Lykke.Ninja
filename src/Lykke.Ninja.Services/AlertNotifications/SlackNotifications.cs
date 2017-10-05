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

        public async Task SendError(string sender,  string message)
        {

            await _slackClient.SendAsync("Errors", "Lykke.Ninja", $"{sender}: {message}");
        }

        public Task NotifyAsync(string message)
        {
            return SendError("PoisonQueueNotifier", message);
        }
    }
}
