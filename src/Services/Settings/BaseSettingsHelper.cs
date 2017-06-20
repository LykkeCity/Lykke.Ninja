using System;
using System.Collections.Generic;
using System.Text;
using Core.Settings;
using NBitcoin;

namespace Services.Settings
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
