using Autofac;
using Common.Log;
using Core.Settings;
using InitialParser.SetSpendable.Functions;

namespace InitialParser.SetSpendable.Binders
{
    public static class SetSpendableFunctionsBinder
    {
        public static void BindParserFunctions(this ContainerBuilder ioc,
            BaseSettings settings,
            ILog log)
        {

            ioc.RegisterType<SetSpendableFunctions>().AsSelf();
        }
    }
}
