using System;
using System.Runtime.InteropServices;
using System.Text;

namespace NORMAX.Wallpaper
{
    /// <summary>
    /// Implements the well-known "WorkerW" trick used by wallpaper utilities to place a
    /// window directly on the desktop, behind the icons and behind all normal application
    /// windows, without altering the actual Windows wallpaper.
    ///
    /// If this fails for any reason (blocked by a Windows update, unusual shell configuration,
    /// etc.) the caller falls back to a plain bottom-most window - see WallpaperWindow.PushToDesktop.
    /// </summary>
    public static class DesktopWallpaperInterop
    {
        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string lpClassName, string? lpWindowName);

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam,
            uint fuFlags, uint uTimeout, out IntPtr lpdwResult);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string? lpszClass, string? lpszWindow);

        [DllImport("user32.dll")]
        private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll")]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        private const uint SMTO_NORMAL = 0x0;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOACTIVATE = 0x0010;
        private static readonly IntPtr HWND_BOTTOM = new(1);

        /// <summary>
        /// Attempts to reparent <paramref name="hwnd"/> onto the desktop's WorkerW layer.
        /// Returns true on success.
        /// </summary>
        public static bool TryAttachToDesktop(IntPtr hwnd)
        {
            try
            {
                IntPtr progman = FindWindow("Progman", null);
                if (progman == IntPtr.Zero) return false;

                // Ask Progman to spawn a WorkerW behind the desktop icons. Undocumented but
                // stable and widely used (0x052C) since Windows 7 through Windows 11.
                SendMessageTimeout(progman, 0x052C, IntPtr.Zero, IntPtr.Zero, SMTO_NORMAL, 1000, out _);

                IntPtr workerW = IntPtr.Zero;

                EnumWindows((topHandle, _) =>
                {
                    IntPtr shellView = FindWindowEx(topHandle, IntPtr.Zero, "SHELLDLL_DefView", null);
                    if (shellView != IntPtr.Zero)
                    {
                        // The WorkerW we want is the *next* sibling of the one hosting SHELLDLL_DefView.
                        workerW = FindWindowEx(IntPtr.Zero, topHandle, "WorkerW", null);
                    }
                    return true; // keep enumerating
                }, IntPtr.Zero);

                if (workerW == IntPtr.Zero)
                {
                    // Some Windows 11 builds host the icons directly under Progman and never
                    // create a second WorkerW until asked again - retry once.
                    SendMessageTimeout(progman, 0x052C, IntPtr.Zero, IntPtr.Zero, SMTO_NORMAL, 1000, out _);
                    EnumWindows((topHandle, _) =>
                    {
                        IntPtr shellView = FindWindowEx(topHandle, IntPtr.Zero, "SHELLDLL_DefView", null);
                        if (shellView != IntPtr.Zero)
                            workerW = FindWindowEx(IntPtr.Zero, topHandle, "WorkerW", null);
                        return true;
                    }, IntPtr.Zero);
                }

                if (workerW == IntPtr.Zero) return false;

                SetParent(hwnd, workerW);
                SetWindowPos(hwnd, HWND_BOTTOM, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_NOACTIVATE);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
