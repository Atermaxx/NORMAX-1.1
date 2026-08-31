using System;
using System.IO;
using System.Text.Json;

namespace NORMAX.Settings
{
    /// <summary>Loads/saves <see cref="AppSettings"/> to %AppData%\NORMAX\settings.json.</summary>
    public class SettingsService
    {
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        public string SettingsDirectory { get; }
        public string SettingsFilePath { get; }

        public AppSettings Current { get; private set; }

        public SettingsService()
        {
            SettingsDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NORMAX");
            SettingsFilePath = Path.Combine(SettingsDirectory, "settings.json");
            Current = Load();
        }

        private AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    var json = File.ReadAllText(SettingsFilePath);
                    var loaded = JsonSerializer.Deserialize<AppSettings>(json);
                    if (loaded != null) return loaded;
                }
            }
            catch
            {
                // Corrupt or unreadable settings file - fall back to defaults rather than crash.
            }

            return new AppSettings();
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(SettingsDirectory);
                var json = JsonSerializer.Serialize(Current, JsonOptions);
                File.WriteAllText(SettingsFilePath, json);
            }
            catch
            {
                // Non-fatal: settings just won't persist this run.
            }
        }
    }
}
