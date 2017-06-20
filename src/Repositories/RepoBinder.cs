using Autofac;
using AzureStorage.Tables;
using Common.Log;
using Core.ServiceMonitoring;
using Core.Settings;
using Repositories.ServiceMonitoring;

namespace Repositories
{
    public static class RepoBinder
    {
        public static void BindRepositories(this ContainerBuilder ioc, BaseSettings settings, ILog log)
        {
            ioc.BindRepo(settings, log);
            ioc.BindQueue(settings);
        }

        private static void BindRepo(this ContainerBuilder ioc, BaseSettings settings, ILog log)
        {
            ioc.RegisterInstance(new ServiceMonitoringRepository(new AzureTableStorage<MonitoringRecordEntity>(settings.Db.SharedConnString, "Monitoring", log)))
                .As<IServiceMonitoringRepository>();
        }

        private static void BindQueue(this ContainerBuilder ioc, BaseSettings settings)
        {
        }
    }
}
