using Autofac;
using Common.Log;
using Lykke.JobTriggers.Extenstions;
using Lykke.Ninja.Core.Settings;

namespace Lykke.Ninja.BalanceJob.Binders
{
    public static class BackgroundBinder
    {
        public static void BindBackgroundJobs(this ContainerBuilder ioc, 
            BaseSettings settings,
            ILog log)
        {
            ioc.AddTriggers(pool =>
            {
                // default connection must be initialize
                pool.AddDefaultConnection(settings.Db.DataConnString);
            });
        }
    }
}
