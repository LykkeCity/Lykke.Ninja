using System.Net.Http;

namespace Lykke.Ninja.Web.Proxy
{
    //based on https://github.com/aspnet/Proxy/
    public class ProxyOptions
    {
        public string Scheme { get; set; }
        public string Host { get; set; }
        public string Port { get; set; }
        public HttpMessageHandler BackChannelMessageHandler { get; set; }
    }
}
