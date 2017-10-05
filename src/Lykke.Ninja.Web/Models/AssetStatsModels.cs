using Lykke.Ninja.Core;
using Lykke.Ninja.Core.AssetStats;

namespace Lykke.Ninja.Web.Models
{
    public class AssetStatsAddressSummaryViewModel
    {
        public string Address { get; set; }
        public double Balance { get; set; }

        public static AssetStatsAddressSummaryViewModel Create(IAssetStatsAddressSummary source)
        {
            return new AssetStatsAddressSummaryViewModel
            {
                Address = source.Address,
                Balance = source.Balance
            };
        }
    }

    public class AssetStatsTransactionViewModel
    {
        public string Hash { get; set; }

        public static AssetStatsTransactionViewModel Create(IAssetStatsTransaction source)
        {
            if (source == null)
            {
                return null;
            }

            return new AssetStatsTransactionViewModel
            {
                Hash = source.Hash
            };
        }
    }

    public class AssetStatsBlockViewModel
    {
        public int Height { get; set; }

        public static AssetStatsBlockViewModel Create(IAssetStatsBlock source)
        {
            return new AssetStatsBlockViewModel
            {
                Height = source.Height
            };
        }
    }

    public class AddressChangeViewModel
    {
        public string Address { get; set; }
        public double Quantity { get; set; }

        public static AddressChangeViewModel Create(IAddressChange source)
        {
            return new AddressChangeViewModel
            {
                Address = source.Address,
                Quantity = source.Quantity
            };
        }
    }
}
