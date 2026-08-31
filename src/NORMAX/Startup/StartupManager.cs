using System;
using System.Diagnostics;
using Microsoft.Win32;

namespace NORMAX.Startup
{
    /// <summary>Registers/unregisters NORMAX to launch (minimized) when Windows starts, via the
    /// standard per-user Run key. No admin rights required, no scheduled task needed.</summary>
    public static class StartupManager
    {
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string ValueName = "NORMAX";

        public static void SetEnabled(bool enabled)
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
            if (key == null) return;

            if (enabled)
            {
                string? exePath = Process.GetCurrentProcess().MainModule?.FileName;
                if (string.IsNullOrEmpty(exePath)) return;
                key.SetValue(ValueName, $"\"{exePath}\" --minimized");
            }
            else
            {
                if (key.GetValue(ValueName) != null)
                    key.DeleteValue(ValueName, throwOnMissingValue: false);
            }
        }

        public static bool IsEnabled()
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
            return key?.GetValue(ValueName) != null;
        }
    }
}
