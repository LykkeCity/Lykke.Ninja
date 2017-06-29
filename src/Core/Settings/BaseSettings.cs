using System.ComponentModel.DataAnnotations;

namespace Core.Settings
{
    public class BaseSettings
    {
        public BaseSettings()
        {
            Proxy = new ProxySettings();
        }

        [Required]
        public DbSettings Db { get; set; }

        [Required]
        public string NinjaUrl { get; set; }

        [Required]
        public string Network { get; set; }

        [Required]
        public MongoCredentials NinjaData { get; set; }

        public int MaxParseBlockQueuedCommandCount { get; set; } = 100;

        public ProxySettings Proxy { get; set; }
    }

    public class DbSettings
    {
        [Required]
        public string DataConnString { get; set; }
        [Required]
        public string SharedConnString { get; set; }
        [Required]
        public string LogsConnString { get; set; }


    }

    public class ProxySettings
    {
        public bool ProxyAllRequests { get; set; } = false;
    }

    public class MongoCredentials
    {
        [Required]
        public string ConnectionString { get; set; }

        [Required]
        public string DbName { get; set; }
    }
}