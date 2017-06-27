using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.ParseBlockCommand;
using Core.Settings;
using Microsoft.AspNetCore.Mvc;
using Services.Ninja;
using Services.Settings;
using Web.Models;

namespace Web.Controllers
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
