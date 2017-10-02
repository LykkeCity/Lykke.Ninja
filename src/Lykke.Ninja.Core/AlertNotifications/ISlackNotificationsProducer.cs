using System.Threading.Tasks;

namespace Lykke.Ninja.Core.AlertNotifications
{
    public class SlackNotificationRequestMsg
    {
        public string Sender { get; set; }
        public string Type { get; set; }
        public string Message { get; set; }
    }

    public interface ISlackNotificationsProducer
    {
        Task SendError(string type, string message);
    }
}
