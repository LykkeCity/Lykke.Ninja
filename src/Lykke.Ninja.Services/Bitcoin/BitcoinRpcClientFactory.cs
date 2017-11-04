using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Lykke.Ninja.Core.Bitcoin;
using Lykke.Ninja.Core.Settings;
using NBitcoin.RPC;

namespace Lykke.Ninja.Services.Bitcoin
{
    public class BitcoinRpcClientFactory: IBitcoinRpcClientFactory
    {
        private readonly BaseSettings _settings;

        public BitcoinRpcClientFactory(BaseSettings settings)
        {
            _settings = settings;
        }

        public RPCClient GetClient()
        {
            return new RPCClient(new NetworkCredential(_settings.BitcoinRpc.Username, 
                _settings.BitcoinRpc.Password),
                _settings.BitcoinRpc.IpAddress, _settings.UsedNetwork());
        }
    }
}
