using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace NORMAX.Wallpaper
{
    public record MonitorInfo(int Index, string DeviceName, bool IsPrimary, Rect Bounds);

    public struct Rect
    {
        public int Left, Top, Right, Bottom;
        public int Width => Right - Left;
        public int Height => Bottom - Top;
    }

    /// <summary>Enumerates connected displays via Win32 so we don't need a WinForms reference.</summary>
    public static class MonitorHelper
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct RECT { public int left, top, right, bottom; }

        [StructLayout(LayoutKind.Sequential)]
        private struct MONITORINFOEX
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public int dwFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string szDevice;
        }

        private delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);

        [DllImport("user32.dll")]
        private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX lpmi);

        private const int MONITORINFOF_PRIMARY = 0x1;

        public static List<MonitorInfo> GetMonitors()
        {
            var result = new List<MonitorInfo>();
            int index = 0;

            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr hMonitor, IntPtr _, ref RECT rect, IntPtr _) =>
            {
                var mi = new MONITORINFOEX { cbSize = Marshal.SizeOf<MONITORINFOEX>() };
                if (GetMonitorInfo(hMonitor, ref mi))
                {
                    bool isPrimary = (mi.dwFlags & MONITORINFOF_PRIMARY) != 0;
                    result.Add(new MonitorInfo(
                        index,
                        mi.szDevice,
                        isPrimary,
                        new Rect { Left = mi.rcMonitor.left, Top = mi.rcMonitor.top, Right = mi.rcMonitor.right, Bottom = mi.rcMonitor.bottom }));
                    index++;
                }
                return true;
            }, IntPtr.Zero);

            // Primary monitor first, for a stable default.
            result.Sort((a, b) => b.IsPrimary.CompareTo(a.IsPrimary));
            for (int i = 0; i < result.Count; i++)
            {
                result[i] = result[i] with { Index = i };
            }
            return result;
        }

        public static MonitorInfo? GetByIndex(int index)
        {
            var monitors = GetMonitors();
            if (monitors.Count == 0) return null;
            return index >= 0 && index < monitors.Count ? monitors[index] : monitors[0];
        }
    }
}
