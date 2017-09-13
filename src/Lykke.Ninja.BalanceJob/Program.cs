using System;
using System.Runtime.Loader;
using Autofac.Extensions.DependencyInjection;
using Lykke.Ninja.Core.Settings;
using Lykke.Ninja.Core.Settings.Validation;
using Lykke.JobTriggers.Triggers;
using System.Threading;
using Lykke.Ninja.BalanceJob;
using Microsoft.Extensions.Configuration;

namespace Lykke.Ninja.Jobs
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Clear();
            Console.Title = "Lykke Ninja Balance job - Ver. " + Microsoft.Extensions.PlatformAbstractions.PlatformServices.Default.Application.ApplicationVersion;

            var host = new AppHost();

            Console.WriteLine("Lykke Ninja Balance job is running");
            Console.WriteLine("Utc time: " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));

            host.Run();
        }
        
    }
}