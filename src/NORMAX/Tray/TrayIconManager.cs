using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
using Hardcodet.Wpf.TaskbarNotification;

namespace NORMAX.Tray
{
    /// <summary>Owns the notify-icon and its context menu so NORMAX stays reachable while minimized.</summary>
    public class TrayIconManager : IDisposable
    {
        private readonly TaskbarIcon _icon;

        public TrayIconManager()
        {
            _icon = new TaskbarIcon
            {
                ToolTipText = "NORMAX",
                Visibility = Visibility.Visible
            };

            try
            {
                var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "normax.ico");
                if (File.Exists(iconPath))
                    _icon.IconSource = new BitmapImage(new Uri(iconPath, UriKind.Absolute));
            }
            catch { /* icon is cosmetic - never let it block startup */ }

            _icon.TrayMouseDoubleClick += (_, _) => OpenMainWindow();
            _icon.ContextMenu = BuildMenu();
        }

        private System.Windows.Controls.ContextMenu BuildMenu()
        {
            var menu = new System.Windows.Controls.ContextMenu();

            AddItem(menu, "Open NORMAX", (_, _) => OpenMainWindow());
            menu.Items.Add(new System.Windows.Controls.Separator());
            AddItem(menu, "Play / Pause", (_, _) => App.Engine.TogglePlayPause());
            AddItem(menu, "Next Video", (_, _) => App.Engine.Next());
            AddItem(menu, "Previous Video", (_, _) => App.Engine.Previous());
            AddItem(menu, "Mute / Unmute", (_, _) =>
            {
                App.Engine.Muted = !App.Engine.Muted;
                App.SettingsService.Current.Muted = App.Engine.Muted;
                App.SettingsService.Save();
            });
            menu.Items.Add(new System.Windows.Controls.Separator());
            AddItem(menu, "Stop Background", (_, _) => App.Wallpaper?.StopAndClose());
            AddItem(menu, "Exit NORMAX", (_, _) => App.ShutdownApplication());

            return menu;
        }

        private static void AddItem(System.Windows.Controls.ContextMenu menu, string header, RoutedEventHandler handler)
        {
            var item = new System.Windows.Controls.MenuItem { Header = header };
            item.Click += handler;
            menu.Items.Add(item);
        }

        private static void OpenMainWindow()
        {
            var main = Application.Current.Windows.OfType<NORMAX.MainWindow>().FirstOrDefault();
            if (main == null)
            {
                main = new NORMAX.MainWindow();
            }
            main.Show();
            main.WindowState = WindowState.Normal;
            main.Activate();
        }

        public void Dispose()
        {
            _icon.Dispose();
        }
    }
}
