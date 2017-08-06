using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Ninja.Core.ParseBlockCommand;
using Lykke.Ninja.Core.Settings;
using Microsoft.AspNetCore.Mvc;
using Lykke.Ninja.Services.Ninja;
using Lykke.Ninja.Web.Models;

namespace Lykke.Ninja.Web.Controllers
{
    [Route("addresses")]
    public class FixAddressController:Controller
    {
        private readonly IFixAddressCommandProducer _commandProducer;
        private readonly BaseSettings _baseSettings;

        public FixAddressController(IFixAddressCommandProducer commandProducer, BaseSettings baseSettings)
        {
            _commandProducer = commandProducer;
            _baseSettings = baseSettings;
        }

        #if DEBUG

        [HttpGet("fix/{address}")]
        public async Task<CommandResult> ProduceCommand(string address)
        {
            try
            {
                await _commandProducer.CreateFixAddressCommand(
                    BitcoinAddressHelper.GetBitcoinAddress(address, _baseSettings.UsedNetwork()));

                return CommandResultBuilder.Ok();
            }
            catch (Exception)
            {
                return CommandResultBuilder.Fail("Inner exception");
            }
        }
        
        #endif

    }
}
