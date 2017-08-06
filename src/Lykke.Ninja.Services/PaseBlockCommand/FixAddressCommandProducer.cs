using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Lykke.Ninja.Core.ParseBlockCommand;
using NBitcoin;

namespace Lykke.Ninja.Services.PaseBlockCommand
{
    public class FixAddressCommandProducer: IFixAddressCommandProducer
    {
        public Task CreateFixAddressCommand(BitcoinAddress address)
        {
            throw new NotImplementedException();
        }
    }
}
