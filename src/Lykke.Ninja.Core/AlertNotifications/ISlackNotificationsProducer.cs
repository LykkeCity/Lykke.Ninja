using System.Threading.Tasks;

namespace Core.AlertNotifications
{
    public class SlackNotificationRequestMsg
    {
        public string Sender { get; set; }
        public string Type { get; set; }
        public string Message { get; set; }
    }

    public interface ISlackNotificationsProducer
    {
        Task SendNotification(string type, string message, string sender);
    }
}
