using Autofac;
using Common.Log;
using Core.Settings;
using InitialParser.Functions;

namespace InitialParser.Binders
{
    public static class InitialParserFunctionsBinder
    {
        public static void BindParserFunctions(this ContainerBuilder ioc,
            BaseSettings settings,
            ILog log)
        {

            ioc.RegisterType<InitialParserFunctions>().AsSelf();
        }
    }
}
