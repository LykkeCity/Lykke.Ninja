using System;
using Common.Log;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Lykke.Ninja.Web.Filters
{
    public class HandleAllExceptionsFilterFactory : IFilterFactory
    {
        public bool IsReusable => true;

        public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
        {
            var log = (ILog)serviceProvider.GetService(typeof(ILog));
            return new HandleAllExceptionsFilter(log);
        }
    }
}
