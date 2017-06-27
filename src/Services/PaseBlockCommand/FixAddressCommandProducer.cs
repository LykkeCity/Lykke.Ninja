using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Core.ParseBlockCommand;
using NBitcoin;

namespace Services.PaseBlockCommand
{
    public class FixAddressCommandProducer: IFixAddressCommandProducer
    {
        public Task CreateFixAddressCommand(BitcoinAddress address)
        {
            throw new NotImplementedException();
        }
    }
}
