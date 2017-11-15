using System;
using NBitcoin;
using QBitNinja.Client.Models;

namespace Lykke.Ninja.Services.Ninja
{
    public class BitcoinAddressHelper
    {
        public static BitcoinAddress GetBitcoinAddress(string data, Network network)
        {
            try
            {
                var b58 = Network.Parse<IBase58Data>(data, network);
                if (b58 != null)
                {
                    if (b58 is BitcoinAddress)
                    {
                        return (BitcoinAddress)b58;
                    }
                    if (b58 is BitcoinColoredAddress)
                    {
                        return ((BitcoinColoredAddress)b58).Address;
                    }
                }
            }
            catch (FormatException) { }

            try
            {
                var b32 = Network.Parse<IBech32Data>(data, null);
                if (b32 != null)
                {
                    switch (b32.Type)
                    {
                        case Bech32Type.WITNESS_PUBKEY_ADDRESS:
                        case Bech32Type.WITNESS_SCRIPT_ADDRESS:
                            return (BitcoinAddress)b32;
                        default:
                            throw new FormatException("Invalid bech32 string");
                    }

                }
            }
            catch (FormatException) { }
            throw new FormatException("Not a base58 or bech32");
            
        }

        
    }
}
