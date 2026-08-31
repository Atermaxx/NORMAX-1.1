using System;
using System.Windows;
using LibVLCSharp.Shared;
using NORMAX.Playback;
using NORMAX.Settings;
using NORMAX.Tray;
using NORMAX.Wallpaper;

namespace NORMAX
{
    /// <summary>
    /// Composition root. Owns the long-lived services (settings, playback engine,
    /// wallpaper window, tray icon) so that closing the main window never kills
    /// the background playback.
    /// </summary>
    public partial class App : Application
    {
        public static SettingsService SettingsService { get; private set; } = null!;
        public static PlaylistManager Playlist { get; private set; } = null!;
        public static PlaybackEngine Engine { get; private set; } = null!;
        public static WallpaperWindow? Wallpaper { get; private set; }
        public static TrayIconManager? Tray { get; private set; }
        public static bool IsShuttingDown { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // The app keeps running via the tray icon even if every window closes.
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            // Safety net: an unexpected exception anywhere in the UI (e.g. while opening
            // Settings) shows a message instead of taking the whole app down.
            DispatcherUnhandledException += (_, ex) =>
            {
                MessageBox.Show(
                    "NORMAX ran into a problem but will keep running:\n\n" + ex.Exception.Message,
                    "NORMAX", MessageBoxButton.OK, MessageBoxImage.Warning);
                ex.Handled = true;
            };

            try
            {
                Core.Initialize();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "NORMAX could not initialize its video engine (LibVLC).\n\n" + ex.Message,
                    "NORMAX - Startup error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
                return;
            }

            SettingsService = new SettingsService();
            var settings = SettingsService.Current;

            Playlist = new PlaylistManager();
            Engine = new PlaybackEngine(Playlist);
            Engine.Volume = settings.Volume;
            Engine.Muted = settings.Muted;

            Tray = new TrayIconManager();

            bool startMinimized = Array.Exists(e.Args, a => a.Equals("--minimized", StringComparison.OrdinalIgnoreCase));

            var main = new MainWindow();
            if (!startMinimized)
            {
                main.Show();
            }

            // Restore last folder / auto-start playback if configured.
            if (settings.RememberLastFolder && !string.IsNullOrWhiteSpace(settings.FolderPath)
                && System.IO.Directory.Exists(settings.FolderPath))
            {
                main.LoadFolder(settings.FolderPath, announce: false);

                if (settings.AutoStartPlayback)
                {
                    main.StartBackground();
                }
            }
        }

        public static void ShutdownApplication()
        {
            IsShuttingDown = true;
            try { Wallpaper?.StopAndClose(); } catch { /* best-effort */ }
            try { Engine?.Dispose(); } catch { /* best-effort */ }
            try { Tray?.Dispose(); } catch { /* best-effort */ }
            Current.Shutdown();
        }

        public static void EnsureWallpaperWindow()
        {
            Wallpaper ??= new WallpaperWindow();
        }
    }
}
