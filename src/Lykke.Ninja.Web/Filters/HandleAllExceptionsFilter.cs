using Common;
using Common.Log;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Lykke.Ninja.Web.Filters
{
    public class HandleAllExceptionsFilter : IExceptionFilter
    {
        private readonly ILog _logger;

        public HandleAllExceptionsFilter(ILog logger)
        {
            _logger = logger;
        }

        public void OnException(ExceptionContext context)
        {
            var controller = context.RouteData.Values["controller"].ToString();
            var action = context.RouteData.Values["action"].ToString();
            var request = context.HttpContext.Request;


            _logger.WriteErrorAsync(action, controller, new
                {
                    Path = request.Path.HasValue ? request.Path.Value : "",
                    request.Query,
                    request.Method,
                }.ToJson(), context.Exception)
                .Wait();
        }
    }
}
