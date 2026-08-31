using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace NORMAX.Wallpaper
{
    /// <summary>
    /// The actual "desktop background" surface: a borderless, full-monitor, pure-black
    /// window with a centered video panel sized to a percentage of the screen.
    /// It is reparented behind the desktop icons via <see cref="DesktopWallpaperInterop"/>
    /// whenever possible, with a bottom-of-Z-order fallback if that fails.
    /// </summary>
    public partial class WallpaperWindow : Window
    {
        [DllImport("user32.dll")]
        private static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        private static readonly IntPtr HWND_BOTTOM = new(1);
        private const uint SWP_NOSIZE = 0x0001, SWP_NOMOVE = 0x0002, SWP_NOACTIVATE = 0x0010;

        private DispatcherTimer? _keepBehindTimer;
        private int _videoPercentage = 82;
        private double _offsetXPercent;
        private double _offsetYPercent;
        private bool _attachedToDesktop;
        private bool _allowClose;

        // --- Win32 message constants used to make this window immune to Win+D / Alt+F4 ---
        private const int WM_SYSCOMMAND = 0x0112;
        private const int SC_MINIMIZE = 0xF020;
        private const int SC_CLOSE = 0xF060;

        public WallpaperWindow()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            // NOTE: sizing is driven off the window's own ActualWidth/ActualHeight (WPF's
            // DPI-correct layout units), never off raw Win32 pixel rectangles - mixing the
            // two was the root cause of the video panel being off-center/mis-proportioned
            // on any monitor that isn't running at 100% scaling.
            SizeChanged += (_, _) => UpdatePanelSize();
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            try
            {
                VideoSurface.MediaPlayer = App.Engine.MediaPlayer;

                var settings = App.SettingsService.Current;
                _videoPercentage = settings.VideoPercentage;
                _offsetXPercent = settings.HorizontalOffsetPercent;
                _offsetYPercent = settings.VerticalOffsetPercent;

                ApplyMonitor(settings.MonitorIndex);

                var hwnd = new WindowInteropHelper(this).Handle;
                HwndSource.FromHwnd(hwnd)?.AddHook(BlockSystemCommandsHook);
            }
            catch (Exception ex)
            {
                MessageBox.Show("NORMAX could not start the desktop background:\n\n" + ex.Message,
                    "NORMAX", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Swallows the Windows messages behind Win+D ("show desktop", which minimizes every
        /// top-level window) and Alt+F4, so the video layer stays put no matter what the user
        /// does with other windows. The normal NORMAX control window is unaffected - it still
        /// minimizes/hides normally, only this background surface is protected.
        /// </summary>
        private IntPtr BlockSystemCommandsHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_SYSCOMMAND)
            {
                int command = wParam.ToInt32() & 0xFFF0;
                if (command == SC_MINIMIZE || command == SC_CLOSE)
                {
                    handled = true;
                    return IntPtr.Zero;
                }
            }
            return IntPtr.Zero;
        }

        /// <summary>Moves the wallpaper window onto the given monitor (by <see cref="MonitorHelper"/> index).</summary>
        public void ApplyMonitor(int monitorIndex)
        {
            var monitor = MonitorHelper.GetByIndex(monitorIndex);
            if (monitor == null) return;

            var hwnd = new WindowInteropHelper(this).EnsureHandle();
            MoveWindow(hwnd, monitor.Bounds.Left, monitor.Bounds.Top, monitor.Bounds.Width, monitor.Bounds.Height, true);

            if (!_attachedToDesktop)
            {
                _attachedToDesktop = DesktopWallpaperInterop.TryAttachToDesktop(hwnd);
                if (!_attachedToDesktop)
                {
                    StartFallbackKeepBehind(hwnd);
                }
            }

            // The window's ActualWidth/ActualHeight will update asynchronously after MoveWindow;
            // UpdatePanelSize() also re-runs from the SizeChanged handler once that happens.
            UpdatePanelSize();
        }

        public void ApplyVideoPercentage(int percentage)
        {
            _videoPercentage = Math.Clamp(percentage, 50, 95);
            UpdatePanelSize();
        }

        /// <summary>Shifts the video panel away from dead-center, as a percentage of the window size.</summary>
        public void ApplyOffset(double horizontalPercent, double verticalPercent)
        {
            _offsetXPercent = Math.Clamp(horizontalPercent, -20, 20);
            _offsetYPercent = Math.Clamp(verticalPercent, -20, 20);
            UpdatePanelSize();
        }

        private void UpdatePanelSize()
        {
            // ActualWidth/ActualHeight are WPF DIPs, already correct for this window's monitor
            // and DPI - this is what keeps the panel exactly centered and correctly proportioned.
            double w = ActualWidth;
            double h = ActualHeight;
            if (w <= 0 || h <= 0) return;

            double pct = _videoPercentage / 100.0;
            VideoPanel.Width = w * pct;
            VideoPanel.Height = h * pct;

            VideoPanelOffset.X = w * (_offsetXPercent / 100.0);
            VideoPanelOffset.Y = h * (_offsetYPercent / 100.0);
        }

        private void StartFallbackKeepBehind(IntPtr hwnd)
        {
            // Could not reparent onto the WorkerW desktop layer (unusual shell/config).
            // Keep the window pinned to the very bottom of the normal Z-order instead -
            // still behind every other application window, just not behind the icons.
            SetWindowPos(hwnd, HWND_BOTTOM, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_NOACTIVATE);

            _keepBehindTimer ??= new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            _keepBehindTimer.Tick -= KeepBehindTick;
            _keepBehindTimer.Tick += KeepBehindTick;
            _keepBehindTimer.Start();

            void KeepBehindTick(object? s, EventArgs e)
                => SetWindowPos(hwnd, HWND_BOTTOM, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_NOACTIVATE);
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            // Alt+F4 (or any other close request) on this surface is ignored unless NORMAX
            // itself asked for it via StopAndClose() - the video layer should never vanish
            // just because it happened to have focus when a close shortcut was pressed.
            if (!_allowClose)
            {
                e.Cancel = true;
                return;
            }
            base.OnClosing(e);
        }

        public void StopAndClose()
        {
            _allowClose = true;
            _keepBehindTimer?.Stop();
            try { VideoSurface.MediaPlayer = null; } catch { /* ignore */ }
            Close();
        }
    }
}
