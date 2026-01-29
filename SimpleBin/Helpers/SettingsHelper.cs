using SimpleBin.Extensions;
using SimpleBin.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleBin.Helpers
{
    internal static class SettingsHelper
    {
        public static T Get<T>(Func<Settings, T> getter, T fallback)
        {
            try
            {
                return getter(Settings.Default);
            }
            catch
            {
                Settings.Default.Reset();
                Settings.Default.Save();
                return fallback;
            }
        }

        public static void Save(Action<Settings> setter)
        {
            try
            {
                setter(Settings.Default);
                Settings.Default.Save();
            }
            catch
            {
                MessageBox.Show("Can not save user settings");
                // TODO: Create custom error box. Low priority, because an exceptional situation is extremely unlikely 
            }
        }
    }
}
