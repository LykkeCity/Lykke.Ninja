using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AzureStorage;
using Core.ServiceMonitoring;
using Lykke.MonitoringServiceApiCaller;
using Lykke.MonitoringServiceApiCaller.Models;
using Microsoft.WindowsAzure.Storage.Table;
using IMonitoringService = Core.ServiceMonitoring.IMonitoringService;

namespace Repositories.ServiceMonitoring
{

    public class MonitoringService : IMonitoringService
    {
        private readonly MonitoringServiceFacade _monitoring;

        public MonitoringService(MonitoringServiceFacade monitoring)
        {
            _monitoring = monitoring;
        }

        public async Task WriteRecord(IMonitoringRecord record)
        {
            await _monitoring.Ping(new MonitoringObjectPingModel(record.ServiceName, record.Version));
        }
    }
}
