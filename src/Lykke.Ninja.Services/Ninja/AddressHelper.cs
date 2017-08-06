using System;
using NBitcoin;

namespace Lykke.Ninja.Services.Ninja
{
    public class BitcoinAddressHelper
    {
        public static BitcoinAddress GetBitcoinAddress(string base58, Network network)
        {
            if (IsBitcoinColoredAddress(base58, network))
            {
                return new BitcoinColoredAddress(base58, network).Address;
            }

            if (IsBitcoinPubKeyAddress(base58, network))
            {
                return new BitcoinPubKeyAddress(base58, network);
            }



            if (IsBitcoinScriptAddress(base58, network))
            {
                return new BitcoinScriptAddress(base58, network);
            }

            throw new Exception("Invalid base58 address");
        }
        public static bool IsBitcoinColoredAddress(string base58, Network network)
        {
            try
            {
                var notUsed = new BitcoinColoredAddress(base58, network);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool IsBitcoinPubKeyAddress(string base58, Network network)
        {
            try
            {
                var notUsed = new BitcoinPubKeyAddress(base58, network);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool IsBitcoinScriptAddress(string base58, Network network)
        {
            try
            {
                var notUsed = new BitcoinScriptAddress(base58, network);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }


        public static bool IsAddress(string base58, Network network)
        {
            return IsBitcoinColoredAddress(base58, network) ||
                   IsBitcoinPubKeyAddress(base58, network) ||
                   IsBitcoinScriptAddress(base58, network);
        }
    }
}
