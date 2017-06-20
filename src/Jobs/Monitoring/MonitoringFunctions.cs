using System;
using System.Threading.Tasks;
using Core.ServiceMonitoring;
using Lykke.JobTriggers.Triggers.Attributes;

namespace Jobs.Monitoring
{
    public class MonitoringFunctions
    {
        private readonly IServiceMonitoringRepository _serviceMonitoringRepository;

        public MonitoringFunctions(IServiceMonitoringRepository serviceMonitoringRepository)
        {
            _serviceMonitoringRepository = serviceMonitoringRepository;
        }

        private const string ServiceName = "Lykke.Ninja.Jobs";

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

            await _serviceMonitoringRepository.UpdateOrCreate(record);
        }
    }
}
