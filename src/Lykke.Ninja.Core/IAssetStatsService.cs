using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Lykke.Ninja.Core
{
    public interface IAssetStatsAddressSummary
    {
        string Address { get; }
        double Balance { get; }
    }

    public interface IAssetStatsTransaction
    {
        string Hash { get; }
    }

    public interface IAssetStatsBlock
    {
        int Height { get; }
    }

    public interface IAddressChange
    {
        string Address { get; }

        double Quantity { get; }
    }

    public interface IAssetStatsService
    {
        Task<IEnumerable<IAssetStatsAddressSummary>> GetSummaryAsync(IEnumerable<string> assetIds, int? maxBlockHeight);

        Task<IEnumerable<IAssetStatsTransaction>> GetTransactionsForAssetAsync(IEnumerable<string> assetIds, int? minBlockHeight);

        Task<IAssetStatsTransaction> GetLatestTxAsync(IEnumerable<string> assetIds);        
        
        
        Task<IEnumerable<IAddressChange>> GetAddressQuantityChangesAtBlock(int blockHeight, IEnumerable<string> assetIds);

        /// <summary>
        /// Get blocks where asset change happens
        /// </summary>
        Task<IEnumerable<IAssetStatsBlock>> GetBlocksWithChanges(IEnumerable<string> assetIds);
    }
}
