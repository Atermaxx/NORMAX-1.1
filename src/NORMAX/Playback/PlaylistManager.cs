using System;
using System.Collections.Generic;
using System.IO;

namespace NORMAX.Playback
{
    /// <summary>Holds the ordered list of videos for the current folder and tracks position.</summary>
    public class PlaylistManager
    {
        private readonly List<string> _random = new();
        private readonly Random _rng = new();

        public List<string> Files { get; private set; } = new();
        public int CurrentIndex { get; private set; } = -1;
        public string FolderPath { get; private set; } = string.Empty;

        public bool Loop { get; set; } = true;
        public bool Shuffle { get; set; } = false;

        public event Action<List<string>>? PlaylistChanged;
        public event Action? PlaylistEmpty;

        public string? CurrentFile => (CurrentIndex >= 0 && CurrentIndex < Files.Count) ? Files[CurrentIndex] : null;

        public void LoadFolder(string folderPath)
        {
            FolderPath = folderPath;
            Files = VideoFileScanner.GetVideoFiles(folderPath);
            CurrentIndex = Files.Count > 0 ? 0 : -1;
            PlaylistChanged?.Invoke(Files);

            if (Files.Count == 0)
                PlaylistEmpty?.Invoke();
        }

        public void Refresh()
        {
            if (!string.IsNullOrWhiteSpace(FolderPath))
            {
                var previousCurrent = CurrentFile;
                Files = VideoFileScanner.GetVideoFiles(FolderPath);

                if (previousCurrent != null)
                {
                    int idx = Files.IndexOf(previousCurrent);
                    CurrentIndex = idx >= 0 ? idx : (Files.Count > 0 ? 0 : -1);
                }
                else
                {
                    CurrentIndex = Files.Count > 0 ? 0 : -1;
                }

                PlaylistChanged?.Invoke(Files);
                if (Files.Count == 0) PlaylistEmpty?.Invoke();
            }
        }

        /// <summary>Removes a file that no longer exists on disk and adjusts the current index.</summary>
        public void RemoveMissingFile(string path)
        {
            int idx = Files.IndexOf(path);
            if (idx < 0) return;

            Files.RemoveAt(idx);
            if (CurrentIndex >= Files.Count) CurrentIndex = Files.Count - 1;
            PlaylistChanged?.Invoke(Files);
            if (Files.Count == 0) PlaylistEmpty?.Invoke();
        }

        public string? MoveNext()
        {
            if (Files.Count == 0) return null;

            if (Shuffle)
            {
                CurrentIndex = Files.Count == 1 ? 0 : _rng.Next(Files.Count);
                return CurrentFile;
            }

            if (CurrentIndex + 1 < Files.Count)
            {
                CurrentIndex++;
            }
            else if (Loop)
            {
                CurrentIndex = 0;
            }
            else
            {
                return null; // reached the end, no loop
            }

            return CurrentFile;
        }

        public string? MovePrevious()
        {
            if (Files.Count == 0) return null;

            if (CurrentIndex - 1 >= 0)
                CurrentIndex--;
            else if (Loop)
                CurrentIndex = Files.Count - 1;

            return CurrentFile;
        }

        public void SetIndex(int index)
        {
            if (index >= 0 && index < Files.Count)
                CurrentIndex = index;
        }

        public bool FileExistsOnDisk(string path) => File.Exists(path);
    }
}
