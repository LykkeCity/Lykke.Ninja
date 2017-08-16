using System;

namespace Lykke.Ninja.Services.Ninja
{
    public static class NinjaBlockHelper
    {
        private const string TopBlockIdentifier = "tip";

        public static bool IsTopBlock(string identifier)
        {
            return string.Equals(identifier, TopBlockIdentifier, StringComparison.OrdinalIgnoreCase);
            ;
        }
    }
}
