using System.ComponentModel.DataAnnotations;

namespace Lykke.Ninja.Core.Settings
{
    public class GeneralSettings
    {
        [Required]
        public BaseSettings LykkeNinja { get; set; }

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
	
    public class SlackNotificationSettings
    {
        [Required]
        public AzureQueueSettings AzureQueue { get; set; }
    }


}
