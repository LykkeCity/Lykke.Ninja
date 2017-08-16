using System;
using System.Threading.Tasks;
using Common.Log;

namespace Lykke.Ninja.Services
{
    public static class Retry
    {
        private static TimeSpan[] defaultRetryShedule = new[]
        {
            TimeSpan.Zero,
            TimeSpan.FromMilliseconds(10),
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(3),
            TimeSpan.FromSeconds(4),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(10),
            TimeSpan.FromSeconds(30),
            TimeSpan.FromMinutes(1)
        };

        public static async Task<T> Try<T>(Func<Task<T>> action, TimeSpan[] retryShedule = null, Func<Exception, bool> exceptionFilter = null, ILog logger = null)
        {
            int @try = 0;
            if (exceptionFilter == null)
            {
                exceptionFilter = p => true;
            }

            var shedule = retryShedule ?? defaultRetryShedule;

            var tryCount = shedule.Length - 1;
            while (true)
            {
                try
                {
                    return await action();
                }
                catch (Exception ex)
                {
                    @try++;
                    if (!exceptionFilter(ex) || @try >= tryCount)
                        throw;

                    if (logger != null)
                    {
                        await logger.WriteErrorAsync("Retry", "Try", null, ex);
                    }
                    await Task.Delay(shedule[@try]);
                }
            }
        }


        public static async Task Try(Func<Task> action, TimeSpan[] retryShedule = null, Func<Exception, bool> exceptionFilter = null, ILog logger = null)
        {
            int @try = 0;
            if (exceptionFilter == null)
            {
                exceptionFilter = p => true;
            }

            var shedule = retryShedule ?? defaultRetryShedule;

            var tryCount = shedule.Length - 1;
            while (true)
            {
                try
                {
                    await action();
                    return;
                }
                catch (Exception ex)
                {
                    @try++;
                    if (!exceptionFilter(ex) || @try >= tryCount)
                        throw;

                    if (logger != null)
                    {
                        await logger.WriteErrorAsync("Retry", "Try", null, ex);
                    }
                    await Task.Delay(shedule[@try]);
                }
            }
        }

        public static async Task Try(Func<Task> action, int maxTryCount, Func<Exception, bool> exceptionFilter = null,
            ILog logger = null)
        {
            int @try = 0;
            if (exceptionFilter == null)
            {
                exceptionFilter = p => true;
            }


            while (@try <= maxTryCount)
            {
                try
                {
                    await action();
                    return;
                }
                catch (Exception ex)
                {
                    @try++;
                    if (!exceptionFilter(ex) || @try >= maxTryCount)
                        throw;

                    if (logger != null)
                    {
                        await logger.WriteErrorAsync("Retry", "Try", null, ex);
                    }
                }
            }
        }
    }
}
