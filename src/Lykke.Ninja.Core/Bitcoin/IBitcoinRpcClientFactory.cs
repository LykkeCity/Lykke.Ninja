using System;
using System.Collections.Generic;
using System.Text;
using NBitcoin.RPC;

namespace Lykke.Ninja.Core.Bitcoin
{
    public interface IBitcoinRpcClientFactory
    {
        RPCClient GetClient();
    }
}
