using Autofac;
using Autofac.Builder;
using Common.Log;
using Core.Ninja.Block;
using Core.Settings;
using QBitNinja.Client;
using Services.Ninja.Block;
using Services.Settings;

namespace Services
{
    public static class SrvBinder
    {
        public static void BindCommonServices(this ContainerBuilder ioc, BaseSettings settings, ILog log)
        {

            ioc.RegisterInstance(new QBitNinjaClient(settings.NinjaUrl, settings.UsedNetwork())).As<QBitNinjaClient>();
            ioc.RegisterType<NinjaBlockService>().As<INinjaBlockService>();

        }
    }
}
