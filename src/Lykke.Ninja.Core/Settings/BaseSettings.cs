using Lykke.SettingsReader.Attributes;

namespace Lykke.Ninja.Core.Settings
{
    public class BaseSettings
    {
        public BaseSettings()
        {
            Proxy = new ProxySettings();
        }

        
        [Optional]
        public int ItemsOnAddressTransactionPage { get; set; } = 500;


        public DbSettings Db { get; set; }

        public string NinjaUrl { get; set; }


        public string Network { get; set; }

        public MongoCredentials NinjaData { get; set; }

        public MongoCredentials UnconfirmedNinjaData { get; set; }


        public BitcoinRpcSettings BitcoinRpc { get; set; }

        [Optional]
        public int MaxParseBlockQueuedCommandCount { get; set; } = 25;
        [Optional]
        public ProxySettings Proxy { get; set; }

        [Optional]
        public InitialParserSettings InitialParser { get; set; }
        [Optional]
        public int MaxNinjaTopBlockDelay { get; set; } = 10;
    }

    public class BitcoinRpcSettings
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string IpAddress { get; set; }

    }

    public class DbSettings
    {
        public string DataConnString { get; set; }

        public string LogsConnString { get; set; }
    }


    public class InitialParserSettings
    {
        public int StartFromBlockHeight { get; set; }

        public int SemaphoreThreadCount { get; set; }

        public SetSpendableSettings SetSpendable { get; set; }
    }

    public class SetSpendableSettings
    {
        public int ThreadsCount { get; set; }

        public int BatchSize { get; set; }
    }
    public class ProxySettings
    {
        [Optional]
        public bool ProxyAllRequests { get; set; } = false;
    }

    public class MongoCredentials
    {

        public string ConnectionString { get; set; }


        public string DbName { get; set; }
    }
    
}