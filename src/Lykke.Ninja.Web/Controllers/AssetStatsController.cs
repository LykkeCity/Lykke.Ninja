using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Ninja.Core;
using Lykke.Ninja.Core.AssetStats;
using Lykke.Ninja.Core.Ninja.Block;
using Lykke.Ninja.Services.Ninja;
using Lykke.Ninja.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace Lykke.Ninja.Web.Controllers
{
    [Route("assetstats")]
    public class AssetStatsController:Controller
    {
        private readonly IAssetStatsService _assetStatsService;
        private readonly INinjaBlockService _ninjaBlockService;

        public AssetStatsController(IAssetStatsService assetStatsService, 
            INinjaBlockService ninjaBlockService)
        {
            _assetStatsService = assetStatsService;
            _ninjaBlockService = ninjaBlockService;
        }

        [HttpGet("addresses")]
        public async Task<CommandResultWithModel<IEnumerable<AssetStatsAddressSummaryViewModel>>> AddressSummary(
            [FromQuery]IEnumerable<string> assetIds, 
            [FromQuery]string at = null)
        {
            var blockHeight = await GetBlockHeight(at);

            var result = await _assetStatsService.GetSummaryAsync(assetIds, blockHeight);

            return CommandResultBuilder.Ok(result.Select(AssetStatsAddressSummaryViewModel.Create));
        }

        [HttpGet("transactions")]
        public async Task<CommandResultWithModel<IEnumerable<AssetStatsTransactionViewModel>>> Transactions(
            [FromQuery]IEnumerable<string> assetIds, 
            [FromQuery]string from = null)
        {
            var blockHeight = await GetBlockHeight(from);

            var result = await _assetStatsService.GetTransactionsForAssetAsync(assetIds, blockHeight);

            return CommandResultBuilder.Ok(result.Select(AssetStatsTransactionViewModel.Create));
        }

        [HttpGet("transactions/last")]
        public async Task<CommandResultWithModel<AssetStatsTransactionViewModel>> LastTx(
            [FromQuery]IEnumerable<string> assetIds)
        {
            var result = await _assetStatsService.GetLatestTxAsync(assetIds);

            return CommandResultBuilder.Ok(AssetStatsTransactionViewModel.Create(result));
        }

        [HttpGet("addressChanges")]
        public async Task<CommandResultWithModel<IEnumerable<AddressChangeViewModel>>> AddressChangesAtBlock(
            [FromQuery]IEnumerable<string> assetIds,
            [FromQuery]string at)
        {
            var blockHeight = await GetBlockHeight(at);
            if (blockHeight == null)
            {
                return CommandResultBuilder.Fail<IEnumerable<AddressChangeViewModel>>("at  block not found. Pass block height or block id");
            }

            var result = await _assetStatsService.GetAddressQuantityChangesAtBlock(blockHeight.Value, assetIds);

            return CommandResultBuilder.Ok(result.Select(AddressChangeViewModel.Create));
        }

        [HttpGet("blockChanges")]
        public async Task<CommandResultWithModel<IEnumerable<AssetStatsBlockViewModel>>> BlocksWithChanges(
            [FromQuery]IEnumerable<string> assetIds)
        {
            var result = await _assetStatsService.GetBlocksWithChanges(assetIds);

            return CommandResultBuilder.Ok(result.Select(AssetStatsBlockViewModel.Create));
        }

        private async Task<int?> GetBlockHeight(string descriptor)
        {
            if (string.IsNullOrEmpty(descriptor) || NinjaBlockHelper.IsTopBlock(descriptor))
            {
                return null;
            }

            int parsedValue;

            if (int.TryParse(descriptor, out parsedValue))
            {
                return parsedValue;
            }


            var blockHeader = await _ninjaBlockService.GetBlockHeader(descriptor, withRetry: false);

            return blockHeader?.BlockHeight;
        }
    }
}
