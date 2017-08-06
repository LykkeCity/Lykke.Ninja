using System;
using System.Runtime.Loader;
using Autofac.Extensions.DependencyInjection;
using Lykke.Ninja.Core.Settings;
using Lykke.Ninja.Core.Settings.Validation;
using Lykke.Ninja.Jobs.Binders;
using Lykke.JobTriggers.Triggers;
using System.Threading;
using Microsoft.Extensions.Configuration;

namespace Lykke.Ninja.Jobs
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Clear();
            Console.Title = "Lykke Ninja job - Ver. " + Microsoft.Extensions.PlatformAbstractions.PlatformServices.Default.Application.ApplicationVersion;

            var host = new AppHost();

            Console.WriteLine("Lykke Ninja job is running");
            Console.WriteLine("Utc time: " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));

            host.Run();
        }
        
    }
}