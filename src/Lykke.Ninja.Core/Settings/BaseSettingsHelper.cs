using System;
using NBitcoin;

namespace Lykke.Ninja.Core.Settings
{
    public static class BaseSettingsHelper
    {
        public static Network UsedNetwork(this BaseSettings baseSettings)
        {
            try
            {
                return Network.GetNetwork(baseSettings.Network);
            }
            catch (Exception)
            {
                return Network.Main;
            }
        }
    }
}
