using System.ComponentModel.DataAnnotations;

namespace Core.Settings
{
    public class GeneralSettings
    {
        [Required]
        public BaseSettings LykkeNinja { get; set; }

        [Required]
        public MonitoringServiceClientSettings MonitoringServiceClient { get; set; }

        [Required]
        public SlackNotificationSettings SlackNotifications { get; set; }
    }


    public class AzureQueueSettings
    {
        [Required]
        public string ConnectionString { get; set; }

        [Required]
        public string QueueName { get; set; }
    }



    public class MonitoringServiceClientSettings
    {
        [Required]
        public string MonitoringServiceUrl { get; set; }
    }

    public class SlackNotificationSettings
    {
        [Required]
        public AzureQueueSettings AzureQueue { get; set; }
    }


}
