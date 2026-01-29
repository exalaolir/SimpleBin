using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SimpleBin.Helpers
{
    public static class StartupHelper
    {
        private const string _appName = "SimpleBin";
        private const string _registryPath = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";

        static StartupHelper()
        {
            // Use to change Registry key when .exe location is changed
            if (IsInStartup())
            {
                RemoveFromStartup();
                AddToStartup();
            }
        }

        public static void AddToStartup()
        {
            string appPath = Application.ExecutablePath;

            using RegistryKey? rk = Registry.CurrentUser.OpenSubKey(
                _registryPath, true);

            rk?.SetValue(_appName, appPath);

            rk?.Close();
        }

        public static bool IsInStartup()
        {
            using RegistryKey? rk = Registry.CurrentUser.OpenSubKey(
               _registryPath, false);

            string? value = rk?.GetValue(_appName) as string;
            rk?.Close();

            return !string.IsNullOrEmpty(value);
        }

        public static void RemoveFromStartup()
        {
            using RegistryKey? rk = Registry.CurrentUser.OpenSubKey(
              _registryPath, true);

            rk?.DeleteValue(_appName, false);
            rk?.Close();
        }
    }
}
