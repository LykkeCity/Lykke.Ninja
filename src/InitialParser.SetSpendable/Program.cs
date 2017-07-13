using System;

namespace InitialParser.SetSpendable
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Clear();
            Console.Title = "Lykke Ninja Initial parser (set spendable)- Ver. " + Microsoft.Extensions.PlatformAbstractions.PlatformServices.Default.Application.ApplicationVersion;

            var host = new AppHost();

            Console.WriteLine("Lykke Ninja Initial parser (set spendable) is running");
            Console.WriteLine("Utc time: " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));

            host.Run();
        }
    }
}