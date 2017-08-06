using Autofac;
using Common.Log;
using Lykke.Ninja.Core.Settings;
using InitialParse.CheckNotFound.Functions;

namespace InitialParse.CheckNotFound.Binders
{
    public static class SetSpendableFunctionsBinder
    {
        public static void BindParserFunctions(this ContainerBuilder ioc,
            BaseSettings settings,
            ILog log)
        {

            ioc.RegisterType<CheckNotFoundFunctions>().AsSelf();
        }
    }
}
