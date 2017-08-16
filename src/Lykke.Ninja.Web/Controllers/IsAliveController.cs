using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Lykke.Ninja.Core.BlockStatus;
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

        public IsAliveController(ITransactionInputRepository inputRepository, IBlockStatusesRepository blockStatusesRepository)
        {
            _inputRepository = inputRepository;
            _blockStatusesRepository = blockStatusesRepository;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var getFailedBlockCount = _blockStatusesRepository.Count(BlockProcessingStatus.Fail);
            var getNotFoundInputsCount = _inputRepository.Count(SpendProcessedStatus.NotFound);
            await Task.WhenAll(getFailedBlockCount, getNotFoundInputsCount);

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

            return new OkObjectResult(new IsAliveResponse
            {
                Version = Microsoft.Extensions.PlatformAbstractions.PlatformServices.Default.Application.ApplicationVersion,
                IssueIndicators = Enumerable.Empty<IsAliveResponse.IssueIndicator>()
            });
        }
    }
}
