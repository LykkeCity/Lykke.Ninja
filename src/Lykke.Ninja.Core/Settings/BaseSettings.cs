﻿using System.ComponentModel.DataAnnotations;

namespace Lykke.Ninja.Core.Settings
{
    public class BaseSettings
    {
        public BaseSettings()
        {
            Proxy = new ProxySettings();
        }

        public int ItemsOnAddressTransactionPage { get; set; } = 500;

        [Required]
        public DbSettings Db { get; set; }

        [Required]
        public string NinjaUrl { get; set; }

        [Required]
        public string Network { get; set; }

        [Required]
        public MongoCredentials NinjaData { get; set; }

        public int MaxParseBlockQueuedCommandCount { get; set; } = 25;

        public ProxySettings Proxy { get; set; }

        public InitialParserSettings InitialParser { get; set; }
    }

    public class DbSettings
    {
        [Required]
        public string DataConnString { get; set; }
        [Required]
        public string LogsConnString { get; set; }
    }


    public class InitialParserSettings
    {
        public int StartFromBlockHeight { get; set; }

        public int SemaphoreThreadCount { get; set; }

        public bool SetOutputIdIndex { get; set; }
        public bool SetInputIdIndex { get; set; }

        public SetSpendableSettings SetSpendable { get; set; }
    }

    public class SetSpendableSettings
    {
        public int ThreadsCount { get; set; }

        public int BatchSize { get; set; }
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