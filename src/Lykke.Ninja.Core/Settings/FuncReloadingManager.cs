using System;
using System.Threading.Tasks;
using Common;
using Lykke.SettingsReader;
using Microsoft.Extensions.Configuration;

namespace Lykke.Ninja.Core.Settings
{
    public class FuncReloadingManager<T>:ReloadingManagerBase<T>
    {
        private readonly Func<T> _getSettings;
        public FuncReloadingManager(Func<T> getSettings)
        {
            _getSettings = getSettings;
        }

        protected override Task<T> Load()
        {
            var settings = _getSettings();
            SettingsProcessor.Process<T>(settings.ToJson(true));
            return Task.FromResult(settings);
        }
    }

    public static class SettingsExtensions
    {
        public static IReloadingManager<T> ReadLykkeNinjaSettings<T>(this IConfigurationRoot root)
        {
            return new FuncReloadingManager<T>(() =>
            {
                Console.WriteLine("Reading settings");

                return root.Get<T>();
            });
        }
    }
}
