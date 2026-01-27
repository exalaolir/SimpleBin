using SimpleBin.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleBin.Helpers
{
    public static class AppThemeHelper
    {
        public enum Theme : byte
        {
            System = 0,
            Dark,
            Light
        }

        public static void Initialize()
        {
            var theme = SettingsHelper.Get(s => (Theme)s.Theme, Theme.System);
            Application.SetColorMode(theme.ToSystemColorMode());
        }

        public static Theme GetAppTheme() => (Theme)SettingsHelper.Get(s => s.Theme, (byte)Theme.System);

        public static void SetTheme(Theme theme)
        {
            SettingsHelper.Save(s => s.Theme = (byte)theme);
            Application.SetColorMode(theme.ToSystemColorMode());
        }
    }
}
