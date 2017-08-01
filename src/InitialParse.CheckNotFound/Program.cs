using System;
using InitialParser.SetSpendable;

namespace InitialParse.CheckNotFound
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Clear();
            Console.Title = "Lykke Ninja Initial parser (CheckNotFound)- Ver. " + Microsoft.Extensions.PlatformAbstractions.PlatformServices.Default.Application.ApplicationVersion;

            var host = new AppHost();

            Console.WriteLine("Lykke Ninja Initial parser (CheckNotFound) is running");
            Console.WriteLine("Utc time: " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));

            host.Run();
        }
    }
}