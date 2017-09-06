﻿using System;
using System.IO;
using Microsoft.AspNetCore.Hosting;

namespace Lykke.Ninja.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine($"Lykke.Ninja version {Microsoft.Extensions.PlatformAbstractions.PlatformServices.Default.Application.ApplicationVersion}");
#if DEBUG
            Console.WriteLine("Is DEBUG");
#else
            Console.WriteLine("Is RELEASE");
#endif           
            var host = new WebHostBuilder()
                .UseKestrel()
                .UseUrls("http://*:80")
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseStartup<Startup>()
                .UseApplicationInsights()
                .Build();

            host.Run();

            Console.WriteLine("Terminated");
        }
    }
}
