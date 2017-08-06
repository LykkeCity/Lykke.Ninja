using System;
using Common.Log;

namespace Core.Settings.Validation
{
    public static class GeneralSettingsValidator
    {
        public static void Validate<T>(T settings, ILog log = null)
        {
            try
            {
                if (settings == null)
                {
                    throw new NullReferenceException("Settings not provided");
                }

                ValidationHelper.ValidateObjectRecursive(settings);
            }
            catch (Exception e)
            {
                log?.WriteFatalErrorAsync("GeneralSettings", "Validation", null, e);

                throw;
            }
        }
    }
}
