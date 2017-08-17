using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Lykke.Ninja.Core.BlockStatus;
using Lykke.Ninja.Core.Ninja.Block;
using Lykke.Ninja.Core.Settings;
using Lykke.Ninja.Core.Transaction;
using Lykke.Ninja.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace Lykke.Ninja.Web.Controllers
{
    [Route("isalive")]
    public class IsAliveController:Controller
    {

        private readonly ITransactionInputRepository _inputRepository;
        private readonly IBlockStatusesRepository _blockStatusesRepository;
        private readonly INinjaBlockService _ninjaBlockService;
        private readonly BaseSettings _baseSettings;

        public IsAliveController(ITransactionInputRepository inputRepository, 
            IBlockStatusesRepository blockStatusesRepository, 
            INinjaBlockService ninjaBlockService, 
            BaseSettings baseSettings)
        {
            _inputRepository = inputRepository;
            _blockStatusesRepository = blockStatusesRepository;
            _ninjaBlockService = ninjaBlockService;
            _baseSettings = baseSettings;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var getNinjaTopHeader = _ninjaBlockService.GetTip(false);
            var getFailedBlockCount = _blockStatusesRepository.Count(BlockProcessingStatus.Fail);
            var getNotFoundInputsCount = _inputRepository.Count(SpendProcessedStatus.NotFound);
            var getLastSuccesfullyProcessedBlockHeight =
                _blockStatusesRepository.GetLastBlockHeight(BlockProcessingStatus.Done);

            await Task.WhenAll(getFailedBlockCount, getNotFoundInputsCount, getNinjaTopHeader, getLastSuccesfullyProcessedBlockHeight);

            var issueIndicators = new List<IsAliveResponse.IssueIndicator>();

            if (getNotFoundInputsCount.Result != 0)
            {
                issueIndicators.Add(new IsAliveResponse.IssueIndicator()
                {
                    Type = "Failed to set outputs spended",
                    Value = $"Failed inputs count {getNotFoundInputsCount.Result}"
                });
            }

            if (getFailedBlockCount.Result != 0)
            {
                issueIndicators.Add(new IsAliveResponse.IssueIndicator()
                {
                    Type = "Failed to parse block",
                    Value = $"Failed blocks count {getFailedBlockCount.Result}"
                });
            }

            var ninjaTopDelay = getNinjaTopHeader.Result.BlockHeight - getLastSuccesfullyProcessedBlockHeight.Result;

            if (ninjaTopDelay > _baseSettings.MaxNinjaTopBlockDelay)
            {
                issueIndicators.Add(new IsAliveResponse.IssueIndicator()
                {
                    Type = "Critical ninja top block delay",
                    Value = $"{ninjaTopDelay} bl."
                });
            }
            

            return new OkObjectResult(new IsAliveResponse
            {
                Version = Microsoft.Extensions.PlatformAbstractions.PlatformServices.Default.Application.ApplicationVersion,
                IssueIndicators = Enumerable.Empty<IsAliveResponse.IssueIndicator>()
            });
        }
    }
}
