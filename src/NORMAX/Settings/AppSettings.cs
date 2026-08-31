namespace NORMAX.Settings
{
    /// <summary>Everything NORMAX remembers between launches. Stored as plain JSON, offline only.</summary>
    public class AppSettings
    {
        public string FolderPath { get; set; } = string.Empty;

        /// <summary>Percentage (50-95) of the screen the video panel occupies. Default 82.</summary>
        public int VideoPercentage { get; set; } = 82;

        /// <summary>Shifts the video panel left(-)/right(+) from dead-center, as % of screen width. Default 0.</summary>
        public int HorizontalOffsetPercent { get; set; } = 0;

        /// <summary>Shifts the video panel up(-)/down(+) from dead-center, as % of screen height. Default 0.</summary>
        public int VerticalOffsetPercent { get; set; } = 0;

        public int Volume { get; set; } = 60;
        public bool Muted { get; set; } = false;

        public int MonitorIndex { get; set; } = 0; // 0 = primary

        public bool StartWithWindows { get; set; } = false;
        public bool AutoStartPlayback { get; set; } = true;
        public bool RememberLastFolder { get; set; } = true;

        public bool LoopPlaylist { get; set; } = true;
        public bool ShufflePlaylist { get; set; } = false;

        public bool RememberLastVideo { get; set; } = false;
        public int LastVideoIndex { get; set; } = 0;
    }
}
