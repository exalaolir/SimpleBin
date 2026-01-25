using SimpleBin.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Threading.Tasks;

namespace SimpleBin.Helpers
{
    public class IconHelper
    {
        private ResourceManager _resourceManager;

#pragma warning disable CS8618
        public IconHelper(bool isDarkTheme)
#pragma warning restore CS8618
        {
            SetTheme(isDarkTheme);
        }

        private static string ResourseNameGenerator(bool isDarkTheme)
        {
            return $"SimpleBin.Resources.{(isDarkTheme ? "DarkIcons" : "LightIcons")}";
        }

        public Icon GetEmptyIcon() => (Icon)_resourceManager.GetObject("EmptyBin")!;

        public Icon GetIcon() => (Icon)_resourceManager.GetObject("Bin")!;

        public void SetTheme(bool isDarkTheme)
        {
            _resourceManager = new(ResourseNameGenerator(isDarkTheme),
                Assembly.GetExecutingAssembly());
        }
    }
}
