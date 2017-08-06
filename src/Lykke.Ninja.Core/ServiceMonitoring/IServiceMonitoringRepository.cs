using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lykke.Ninja.Core.ServiceMonitoring
{

    public interface IMonitoringRecord
    {
        string ServiceName { get; }
        DateTime DateTime { get; }
        string Version { get; }
    }

    public class MonitoringRecord : IMonitoringRecord
    {


        public string ServiceName { get; set; }
        public DateTime DateTime { get; set; }
        public string Version { get; set; }

        public static MonitoringRecord Create(string serviceName,  string version)
        {
            return new MonitoringRecord
            {
                ServiceName = serviceName,
                Version = version
            };
        }
    }

    public interface IMonitoringService
    {
        Task WriteRecord(IMonitoringRecord record);
    }
}
