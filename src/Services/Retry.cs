using System;
using System.Threading.Tasks;
using Common.Log;

namespace Services
{
    public static class Retry
    {
        public static async Task<T> Try<T>(Func<Task<T>> action, Func<Exception, bool> exceptionFilter = null, ILog logger = null)
        {
            int @try = 0;
            if (exceptionFilter == null)
            {
                exceptionFilter = p => true;
            }

            var shedule = new[]
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
    }
}
