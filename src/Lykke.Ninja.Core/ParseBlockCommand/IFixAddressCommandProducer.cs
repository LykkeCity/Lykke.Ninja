using System.Threading.Tasks;
using NBitcoin;

namespace Lykke.Ninja.Core.ParseBlockCommand
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
