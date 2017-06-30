using Autofac;
using Common.Log;
using Core.Block;
using Core.Ninja.Block;
using Core.Ninja.Transaction;
using Core.ParseBlockCommand;
using Core.Settings;
using Core.Transaction;
using QBitNinja.Client;
using Services.Block;
using Services.Ninja.Block;
using Services.Ninja.Transaction;
using Services.PaseBlockCommand;

namespace Services
{
    public static class SrvBinder
    {
        public static void BindCommonServices(this ContainerBuilder ioc, BaseSettings settings, ILog log)
        {
            ioc.RegisterInstance(new QBitNinjaClient(settings.NinjaUrl, settings.UsedNetwork()) { Colored = true}).As<QBitNinjaClient>();
            ioc.RegisterType<NinjaBlockService>().As<INinjaBlockService>();
            ioc.RegisterType<NinjaTransactionService>().As<INinjaTransactionService>(); 
            ioc.RegisterType<ParseBlockCommandsService>().As<IParseBlockCommandsService>();
            ioc.RegisterType<BlockService>().As<IBlockService>();
            ioc.RegisterType<ProcessParseBlockCommandFacade>().As<IProcessParseBlockCommandFacade>();
            
        }
    }
}
