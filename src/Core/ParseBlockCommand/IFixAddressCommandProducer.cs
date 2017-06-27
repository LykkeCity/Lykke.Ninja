using System.Threading.Tasks;
using NBitcoin;

namespace Core.ParseBlockCommand
{
    public class FixAddressCommandContext
    {
        public string Address { get; set; }
    }


    public interface IFixAddressCommandProducer
    {
        Task CreateFixAddressCommand(BitcoinAddress address);
    }
}
