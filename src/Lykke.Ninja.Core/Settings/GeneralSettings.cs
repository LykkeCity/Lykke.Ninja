using System.ComponentModel.DataAnnotations;

namespace Lykke.Ninja.Core.Settings
{
    public class GeneralSettings
    {

        public BaseSettings LykkeNinja { get; set; }


        public SlackNotificationSettings SlackNotifications { get; set; }
    }


    public class AzureQueueSettings
    {

        public string ConnectionString { get; set; }

        public string QueueName { get; set; }
    }
	
    public class SlackNotificationSettings
    {

        public AzureQueueSettings AzureQueue { get; set; }
    }


}
