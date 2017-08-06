using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Options;

namespace Lykke.Ninja.Web.Proxy
{
    //based on https://github.com/aspnet/Proxy/
    public static class ProxyExtension
    {
        /// <summary>
        /// Sends request to remote server as specified in options
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        public static IApplicationBuilder RunProxy(this IApplicationBuilder app)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            return app.UseMiddleware<ProxyMiddleware>();
        }

        /// <summary>
        /// Sends request to remote server as specified in options
        /// </summary>
        /// <param name="app"></param>
        /// <param name="options">Options for setting port, host, and scheme</param>
        /// <returns></returns>
        public static IApplicationBuilder RunProxy(this IApplicationBuilder app, ProxyOptions options)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            return app.UseMiddleware<ProxyMiddleware>(Options.Create(options));
        }
    }
}
