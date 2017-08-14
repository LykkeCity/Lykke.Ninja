using System.Linq;
using System.Threading.Tasks;
using Common.Log;
using Lykke.JobTriggers.Triggers.Attributes;
using Lykke.Ninja.Core.ParseBlockCommand;
using Lykke.Ninja.Core.Queue;
using Lykke.Ninja.Core.Settings;
using Lykke.Ninja.Services.Ninja;
using QBitNinja.Client;

namespace Lykke.Ninja.BalanceJob.BlockTasks
{
    public class FixAddressConsumer
    {
        private readonly IParseBlockCommandsService _commandProducer;
        private readonly QBitNinjaClient _ninjaClient;
        private readonly IConsole _console;
        private readonly BaseSettings _baseSettings;

        public FixAddressConsumer(IParseBlockCommandsService commandProducer, 
            QBitNinjaClient ninjaClient,
            IConsole console, 
            BaseSettings baseSettings)
        {
            _commandProducer = commandProducer;
            _ninjaClient = ninjaClient;
            _console = console;
            _baseSettings = baseSettings;
        }


        #if DEBUG

        [QueueTrigger(QueueNames.AddressesToFix, notify:true)]
        public async Task Run(FixAddressCommandContext context)
        {
            _console.WriteLine("FixAddressConsumer Run started");

            var balance = await _ninjaClient.GetBalance(BitcoinAddressHelper.GetBitcoinAddress(context.Address, _baseSettings.UsedNetwork()));

            var heights = balance.Operations.Select(p => p.Height).Distinct().OrderBy(p => p);

            _console.WriteLine("Getting heights done");

            foreach (var height in heights)
            {
                await _commandProducer.ProduceParseBlockCommand(height);
            }

            _console.WriteLine("FixAddressConsumer Run Stop");
        }

        #endif
    }
}
