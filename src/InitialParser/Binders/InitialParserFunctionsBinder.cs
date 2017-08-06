using Autofac;
using Common.Log;
using Lykke.Ninja.Core.Settings;
using InitialParser.Functions;

namespace InitialParser.Binders
{
    public static class InitialParserFunctionsBinder
    {
        public static void BindParserFunctions(this ContainerBuilder ioc,
            BaseSettings settings,
            ILog log)
        {

            ioc.RegisterType<GrabNinjaDataFunctions>().AsSelf();
        }
    }
}
