using Autofac;
using Common.Log;
using Core.Settings;
using Lykke.JobTriggers.Extenstions;

namespace Jobs.Binders
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
