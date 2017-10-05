using System;
using System.Threading.Tasks;
using Lykke.JobTriggers.Triggers.Attributes;
using Lykke.Ninja.Core.ServiceMonitoring;

namespace Lykke.Ninja.BalanceJob.Monitoring
{
    public class MonitoringFunctions
    {
        private readonly IMonitoringService _monitoringService;

        public MonitoringFunctions(IMonitoringService monitoringService)
        {
            _monitoringService = monitoringService;
        }

        private const string ServiceName = "Lykke.Ninja.Jobs.Balances";

        [TimerTrigger("00:00:30")]
        public  async Task WriteMonitoringRecord()
        {
            var now = DateTime.UtcNow;

            var record = new MonitoringRecord
            {
                DateTime = now,
                ServiceName = ServiceName,
                Version = Microsoft.Extensions.PlatformAbstractions.PlatformServices.Default.Application.ApplicationVersion
            };

            await _monitoringService.WriteRecord(record);
        }
    }
}
