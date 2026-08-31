using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace NORMAX.Playback
{
    /// <summary>Scans a folder for supported video files, ignoring everything else.</summary>
    public static class VideoFileScanner
    {
        private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".webm"
        };

        public static List<string> GetVideoFiles(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
                return new List<string>();

            IEnumerable<string> files;
            try
            {
                files = Directory.EnumerateFiles(folderPath)
                    .Where(f => SupportedExtensions.Contains(Path.GetExtension(f)));
            }
            catch (Exception)
            {
                // Folder became inaccessible mid-scan - treat as empty rather than crash.
                return new List<string>();
            }

            // "Alphabetical" but numeric-aware, so 01,02,...,10 sorts correctly
            // instead of 01,02,10,03 as a plain string sort would.
            return files.OrderBy(f => Path.GetFileName(f), new NaturalFileNameComparer()).ToList();
        }

        private sealed class NaturalFileNameComparer : IComparer<string>
        {
            private static readonly Regex ChunkPattern = new(@"\d+|\D+", RegexOptions.Compiled);

            public int Compare(string? x, string? y)
            {
                if (x == null || y == null) return string.CompareOrdinal(x, y);

                var xChunks = ChunkPattern.Matches(x).Select(m => m.Value).ToList();
                var yChunks = ChunkPattern.Matches(y).Select(m => m.Value).ToList();
                int count = Math.Min(xChunks.Count, yChunks.Count);

                for (int i = 0; i < count; i++)
                {
                    string a = xChunks[i], b = yChunks[i];
                    bool aNum = long.TryParse(a, out long aVal);
                    bool bNum = long.TryParse(b, out long bVal);

                    int cmp;
                    if (aNum && bNum)
                        cmp = aVal.CompareTo(bVal);
                    else
                        cmp = string.Compare(a, b, StringComparison.OrdinalIgnoreCase);

                    if (cmp != 0) return cmp;
                }

                return xChunks.Count.CompareTo(yChunks.Count);
            }
        }
    }
}
