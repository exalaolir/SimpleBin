using SimpleBin.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleBin.Helpers
{
    public sealed class AppThemeHelper()
    {
        public enum Theme : byte
        {
            System = 0,
            Dark,
            Light
        }

        public void Initialize()
        {
            var theme = SettingsHelper.Get(s => (Theme)s.Theme, Theme.System);
            Application.SetColorMode(theme.ToSystemColorMode());
        }

        public void SetTheme(Theme theme)
        {
            SettingsHelper.Save(s => s.Theme = (byte)theme);
            Application.SetColorMode(theme.ToSystemColorMode());
        }
    }
}
