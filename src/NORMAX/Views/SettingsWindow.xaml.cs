using System;
using System.Windows;
using System.Windows.Controls;
using NORMAX.Startup;
using NORMAX.Wallpaper;

namespace NORMAX.Views
{
    public partial class SettingsWindow : Window
    {
        private bool _initializing = true;

        public SettingsWindow()
        {
            InitializeComponent();

            try
            {
                var settings = App.SettingsService.Current;

                PercentageSlider.Value = settings.VideoPercentage;
                PercentageLabel.Text = $"{settings.VideoPercentage}%";

                HorizontalOffsetSlider.Value = settings.HorizontalOffsetPercent;
                HorizontalOffsetLabel.Text = $"{settings.HorizontalOffsetPercent}%";
                VerticalOffsetSlider.Value = settings.VerticalOffsetPercent;
                VerticalOffsetLabel.Text = $"{settings.VerticalOffsetPercent}%";

                PopulateMonitors(settings.MonitorIndex);

                StartWithWindowsCheck.IsChecked = settings.StartWithWindows;
                AutoStartPlaybackCheck.IsChecked = settings.AutoStartPlayback;
                RememberFolderCheck.IsChecked = settings.RememberLastFolder;
                LoopPlaylistCheck.IsChecked = settings.LoopPlaylist;
                ShufflePlaylistCheck.IsChecked = settings.ShufflePlaylist;
                RememberLastVideoCheck.IsChecked = settings.RememberLastVideo;
            }
            catch (Exception ex)
            {
                // Never let a settings-population glitch crash the app - just show defaults.
                MessageBox.Show("Some settings could not be loaded and were reset to defaults:\n\n" + ex.Message,
                    "NORMAX", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally
            {
                _initializing = false;
            }
        }

        private void PopulateMonitors(int selectedIndex)
        {
            try
            {
                var monitors = MonitorHelper.GetMonitors();
                foreach (var monitor in monitors)
                {
                    MonitorComboBox.Items.Add(monitor.IsPrimary ? $"Monitor {monitor.Index + 1} (Primary)" : $"Monitor {monitor.Index + 1}");
                }

                if (MonitorComboBox.Items.Count > 0)
                {
                    int index = Math.Max(0, Math.Min(selectedIndex, MonitorComboBox.Items.Count - 1));
                    MonitorComboBox.SelectedIndex = index;
                }
            }
            catch
            {
                // If monitor enumeration fails for any reason, just leave the list empty -
                // the rest of Settings still works fine without it.
            }
        }

        private void PercentageSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_initializing) return;

            try
            {
                int pct = (int)e.NewValue;
                PercentageLabel.Text = $"{pct}%";

                App.SettingsService.Current.VideoPercentage = pct;
                App.SettingsService.Save();
                App.Wallpaper?.ApplyVideoPercentage(pct);
            }
            catch (Exception ex)
            {
                StatusFail("size", ex);
            }
        }

        private void OffsetSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_initializing) return;

            try
            {
                int h = (int)HorizontalOffsetSlider.Value;
                int v = (int)VerticalOffsetSlider.Value;
                HorizontalOffsetLabel.Text = $"{h}%";
                VerticalOffsetLabel.Text = $"{v}%";

                App.SettingsService.Current.HorizontalOffsetPercent = h;
                App.SettingsService.Current.VerticalOffsetPercent = v;
                App.SettingsService.Save();
                App.Wallpaper?.ApplyOffset(h, v);
            }
            catch (Exception ex)
            {
                StatusFail("position", ex);
            }
        }

        private void ResetPosition_Click(object sender, RoutedEventArgs e)
        {
            HorizontalOffsetSlider.Value = 0;
            VerticalOffsetSlider.Value = 0;
        }

        private void MonitorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_initializing || MonitorComboBox.SelectedIndex < 0) return;

            try
            {
                App.SettingsService.Current.MonitorIndex = MonitorComboBox.SelectedIndex;
                App.SettingsService.Save();
                App.Wallpaper?.ApplyMonitor(MonitorComboBox.SelectedIndex);
            }
            catch (Exception ex)
            {
                StatusFail("display", ex);
            }
        }

        private void Setting_Changed(object sender, RoutedEventArgs e)
        {
            if (_initializing) return;

            try
            {
                var settings = App.SettingsService.Current;
                settings.StartWithWindows = StartWithWindowsCheck.IsChecked == true;
                settings.AutoStartPlayback = AutoStartPlaybackCheck.IsChecked == true;
                settings.RememberLastFolder = RememberFolderCheck.IsChecked == true;
                settings.LoopPlaylist = LoopPlaylistCheck.IsChecked == true;
                settings.ShufflePlaylist = ShufflePlaylistCheck.IsChecked == true;
                settings.RememberLastVideo = RememberLastVideoCheck.IsChecked == true;
                App.SettingsService.Save();

                App.Playlist.Loop = settings.LoopPlaylist;
                App.Playlist.Shuffle = settings.ShufflePlaylist;

                StartupManager.SetEnabled(settings.StartWithWindows);
            }
            catch (Exception ex)
            {
                StatusFail("startup/playback", ex);
            }
        }

        private static void StatusFail(string area, Exception ex)
        {
            // Settings failures should never crash NORMAX - surface them and move on.
            MessageBox.Show($"Could not update {area} settings:\n\n{ex.Message}",
                "NORMAX", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void Done_Click(object sender, RoutedEventArgs e) => Close();
    }
}
