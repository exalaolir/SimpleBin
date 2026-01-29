using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SimpleBin.Helpers.AppThemeHelper;

namespace SimpleBin.Extensions
{
    public static class ThemeEnumExtensions
    {
        public static SystemColorMode ToSystemColorMode(this Theme theme) => theme switch
        {
            Theme.System => SystemColorMode.System,
            Theme.Dark => SystemColorMode.Dark,
            Theme.Light => SystemColorMode.Classic,
            _ => throw new NotSupportedException()
        };

        public static Theme ToThemEnum(this SystemColorMode colorMode) => colorMode switch
        {
            SystemColorMode.System => Theme.System,
            SystemColorMode.Dark => Theme.Dark,
            SystemColorMode.Classic => Theme.Light,
            _ => throw new NotSupportedException()
        };
    }
}
