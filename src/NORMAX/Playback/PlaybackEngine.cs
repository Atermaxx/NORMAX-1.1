using System;
using System.IO;
using System.Windows;
using LibVLCSharp.Shared;

namespace NORMAX.Playback
{
    /// <summary>
    /// Wraps a single LibVLC + MediaPlayer instance and drives the playlist.
    /// Only one video is ever loaded at a time; the previous Media is disposed
    /// immediately after switching so memory doesn't grow across a long-running session.
    /// </summary>
    public class PlaybackEngine : IDisposable
    {
        private readonly PlaylistManager _playlist;
        private readonly LibVLC _libVlc;
        private Media? _currentMedia;
        private bool _isDisposed;
        private int _consecutiveFailures;

        public MediaPlayer MediaPlayer { get; }

        public event Action<string?>? CurrentVideoChanged; // full path, or null when stopped
        public event Action<bool>? PlayingStateChanged;    // true = playing
        public event Action<string>? VideoSkipped;         // path of a file that failed to play

        public PlaybackEngine(PlaylistManager playlist)
        {
            _playlist = playlist;

            // Hardware-accelerated output; LibVLC picks the best decoder automatically on Windows (DXVA2/D3D11).
            _libVlc = new LibVLC();
            MediaPlayer = new MediaPlayer(_libVlc);

            MediaPlayer.EndReached += (_, _) => Application.Current?.Dispatcher.BeginInvoke(OnEndReached);
            MediaPlayer.EncounteredError += (_, _) => Application.Current?.Dispatcher.BeginInvoke(OnPlaybackError);
            MediaPlayer.Playing += (_, _) => Application.Current?.Dispatcher.BeginInvoke(() => PlayingStateChanged?.Invoke(true));
            MediaPlayer.Paused += (_, _) => Application.Current?.Dispatcher.BeginInvoke(() => PlayingStateChanged?.Invoke(false));
            MediaPlayer.Stopped += (_, _) => Application.Current?.Dispatcher.BeginInvoke(() => PlayingStateChanged?.Invoke(false));
        }

        public int Volume
        {
            get => MediaPlayer.Volume;
            set => MediaPlayer.Volume = Math.Clamp(value, 0, 100);
        }

        public bool Muted
        {
            get => MediaPlayer.Mute;
            set => MediaPlayer.Mute = value;
        }

        public bool IsPlaying => MediaPlayer.IsPlaying;

        /// <summary>Starts playback at the playlist's current file.</summary>
        public void PlayCurrent()
        {
            var file = _playlist.CurrentFile;
            if (file == null) return;
            PlayFile(file);
        }

        private void PlayFile(string path)
        {
            if (!File.Exists(path))
            {
                // Deleted mid-playlist: drop it and move on.
                _playlist.RemoveMissingFile(path);
                VideoSkipped?.Invoke(path);
                AdvanceAndPlay();
                return;
            }

            try
            {
                var previous = _currentMedia;

                var media = new Media(_libVlc, new Uri(path));
                MediaPlayer.Play(media);
                _currentMedia = media;

                previous?.Dispose(); // release the previous video's resources

                _consecutiveFailures = 0;
                CurrentVideoChanged?.Invoke(path);
            }
            catch (Exception)
            {
                HandleUnplayableFile(path);
            }
        }

        private void OnEndReached()
        {
            AdvanceAndPlay();
        }

        private void OnPlaybackError()
        {
            var failed = _playlist.CurrentFile;
            if (failed != null) HandleUnplayableFile(failed);
        }

        private void HandleUnplayableFile(string path)
        {
            VideoSkipped?.Invoke(path);
            _consecutiveFailures++;

            // Safety valve: if every file in the playlist is broken, stop instead of spinning forever.
            if (_consecutiveFailures >= Math.Max(1, _playlist.Files.Count))
            {
                Stop();
                return;
            }

            AdvanceAndPlay();
        }

        private void AdvanceAndPlay()
        {
            var next = _playlist.MoveNext();
            if (next == null)
            {
                Stop();
                return;
            }
            PlayFile(next);
        }

        public void Pause() => MediaPlayer.Pause();

        public void Resume()
        {
            if (_currentMedia == null) PlayCurrent();
            else MediaPlayer.Play();
        }

        public void TogglePlayPause()
        {
            if (MediaPlayer.IsPlaying) Pause();
            else Resume();
        }

        public void Next()
        {
            var next = _playlist.MoveNext();
            if (next != null) PlayFile(next);
        }

        public void Previous()
        {
            var prev = _playlist.MovePrevious();
            if (prev != null) PlayFile(prev);
        }

        public void RestartCurrent()
        {
            if (MediaPlayer.IsSeekable) MediaPlayer.Time = 0;
            if (!MediaPlayer.IsPlaying) MediaPlayer.Play();
        }

        public void Stop()
        {
            MediaPlayer.Stop();
            CurrentVideoChanged?.Invoke(null);
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            try { MediaPlayer.Stop(); } catch { /* ignore */ }
            _currentMedia?.Dispose();
            MediaPlayer.Dispose();
            _libVlc.Dispose();
        }
    }
}
