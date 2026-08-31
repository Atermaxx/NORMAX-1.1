using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Microsoft.Win32;
using NORMAX.Views;

namespace NORMAX
{
    public partial class MainWindow : Window
    {
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        private bool _isPlaying;

        public MainWindow()
        {
            InitializeComponent();

            SourceInitialized += (_, _) =>
            {
                int useDark = 1;
                var hwnd = new WindowInteropHelper(this).Handle;
                DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useDark, sizeof(int));
            };

            Closing += MainWindow_Closing;

            var settings = App.SettingsService.Current;
            VolumeSlider.Value = settings.Volume;
            MuteButton.Content = settings.Muted ? "\U0001F507" : "\U0001F50A";

            App.Playlist.PlaylistChanged += files => Dispatcher.Invoke(() => UpdateStatusForPlaylist(files.Count));
            App.Playlist.PlaylistEmpty += () => Dispatcher.Invoke(() =>
                StatusText.Text = "No supported video files found in this folder.");

            App.Engine.CurrentVideoChanged += path => Dispatcher.Invoke(() =>
                NowPlayingText.Text = path == null ? "Nothing playing" : Path.GetFileName(path));

            App.Engine.PlayingStateChanged += playing => Dispatcher.Invoke(() =>
            {
                _isPlaying = playing;
                PlayPauseButton.Content = playing ? "\u23F8" : "\u25B6";
            });

            App.Engine.VideoSkipped += path => Dispatcher.Invoke(() =>
                StatusText.Text = $"Skipped unplayable file: {Path.GetFileName(path)}");
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (App.IsShuttingDown) return;

            // Closing the window just hides it - playback keeps running via the tray icon.
            e.Cancel = true;
            Hide();
        }

        private void UpdateStatusForPlaylist(int count)
        {
            StatusText.Text = count == 0
                ? "No supported video files found in this folder."
                : $"{count} video{(count == 1 ? "" : "s")} found.";
        }

        public void LoadFolder(string path, bool announce = true)
        {
            App.Playlist.Loop = App.SettingsService.Current.LoopPlaylist;
            App.Playlist.Shuffle = App.SettingsService.Current.ShufflePlaylist;
            App.Playlist.LoadFolder(path);

            FolderPathText.Text = $"Folder: {path}";
            App.SettingsService.Current.FolderPath = path;
            App.SettingsService.Save();

            if (announce)
                UpdateStatusForPlaylist(App.Playlist.Files.Count);
        }

        public void StartBackground()
        {
            if (App.Playlist.Files.Count == 0)
            {
                StatusText.Text = "Select a folder with videos first.";
                return;
            }

            App.EnsureWallpaperWindow();
            App.Wallpaper!.Show();
            App.Engine.PlayCurrent();
            StartStopButton.Content = "Stop Background";
        }

        private void StopBackground()
        {
            App.Wallpaper?.StopAndClose();
            App.Engine.Stop();
            StartStopButton.Content = "Start Background";
        }

        // --- UI event handlers -------------------------------------------------

        private void SelectFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog { Title = "Select Video Folder" };
            if (dialog.ShowDialog(this) == true)
            {
                if (!Directory.Exists(dialog.FolderName))
                {
                    StatusText.Text = "That folder could not be found.";
                    return;
                }
                LoadFolder(dialog.FolderName);
            }
        }

        private void PlayPauseButton_Click(object sender, RoutedEventArgs e) => App.Engine.TogglePlayPause();
        private void NextButton_Click(object sender, RoutedEventArgs e) => App.Engine.Next();
        private void PreviousButton_Click(object sender, RoutedEventArgs e) => App.Engine.Previous();
        private void RestartButton_Click(object sender, RoutedEventArgs e) => App.Engine.RestartCurrent();

        private void MuteButton_Click(object sender, RoutedEventArgs e)
        {
            App.Engine.Muted = !App.Engine.Muted;
            MuteButton.Content = App.Engine.Muted ? "\U0001F507" : "\U0001F50A";
            App.SettingsService.Current.Muted = App.Engine.Muted;
            App.SettingsService.Save();
        }

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            int vol = (int)e.NewValue;
            App.Engine.Volume = vol;
            App.SettingsService.Current.Volume = vol;
            App.SettingsService.Save();
        }

        private void StartStopButton_Click(object sender, RoutedEventArgs e)
        {
            if (StartStopButton.Content?.ToString() == "Start Background")
                StartBackground();
            else
                StopBackground();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow { Owner = this };
            settingsWindow.ShowDialog();
        }
    }
}
