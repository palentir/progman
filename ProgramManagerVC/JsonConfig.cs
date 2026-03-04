using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace ProgramManagerVC
{
    /// <summary>
    /// JSON-based configuration system for PM.NET
    /// </summary>
    public class JsonConfig
    {
        private static string ConfigFilePath => Path.Combine(Application.StartupPath, "progman64.json");
        private static ProgramConfig _config;
        private static readonly object _lock = new object();

        /// <summary>
        /// Load or create the configuration
        /// </summary>
        public static ProgramConfig Load()
        {
            lock (_lock)
            {
                if (_config == null)
                {
                    if (File.Exists(ConfigFilePath))
                    {
                        try
                        {
                            string json = File.ReadAllText(ConfigFilePath);
                            _config = JsonConvert.DeserializeObject<ProgramConfig>(json) ?? CreateDefault();
                        }
                        catch (Exception ex)
                        {
                            // If JSON is corrupted, create default and backup the old one
                            if (File.Exists(ConfigFilePath))
                            {
                                File.Copy(ConfigFilePath, ConfigFilePath + ".backup", true);
                            }
                            _config = CreateDefault();
                            MessageBox.Show($"Configuration file was corrupted and has been reset. A backup was saved.\nError: {ex.Message}",
                                "Configuration Reset", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                    else
                    {
                        _config = CreateDefault();
                        
                        // Try to migrate from old INI files
                        MigrateFromIniFiles();
                    }
                }
                return _config;
            }
        }

        /// <summary>
        /// Save the configuration to JSON
        /// </summary>
        public static void Save()
        {
            lock (_lock)
            {
                try
                {
                    if (_config != null)
                    {
                        string json = JsonConvert.SerializeObject(_config, Formatting.Indented);
                        File.WriteAllText(ConfigFilePath, json);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving configuration: {ex.Message}",
                        "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        /// <summary>
        /// Create default configuration
        /// </summary>
        private static ProgramConfig CreateDefault()
        {
            var defaultShortcutsPath = Path.Combine(Application.StartupPath, "Shortcuts");
            
            return new ProgramConfig
            {
                Application = new ApplicationSettings
                {
                    WindowWidth = 800,
                    WindowHeight = 600,
                    WindowX = 100,
                    WindowY = 100,
                    IconSize = 32,
                    UsernameInTitle = 0,
                    ActiveWindow = "",
                    CurrentProfile = "Default"
                },
                Profiles = new Dictionary<string, ProfileSettings>
                {
                    ["Default"] = new ProfileSettings
                    {
                        Path = defaultShortcutsPath,
                        IsDefault = true
                    }
                },
                Groups = new Dictionary<string, Dictionary<string, GroupSettings>>
                {
                    ["Default"] = new Dictionary<string, GroupSettings>()
                }
            };
        }

        /// <summary>
        /// Migrate data from old INI files
        /// </summary>
        private static void MigrateFromIniFiles()
        {
            try
            {
                var progmanIniPath = Path.Combine(Application.StartupPath, "progman64.ini");
                var defaultIniPath = Path.Combine(Application.StartupPath, "Default.ini");

                // Migrate application settings from progman64.ini
                if (File.Exists(progmanIniPath))
                {
                    MigrateApplicationSettings(progmanIniPath);
                    MigrateProfiles(progmanIniPath);
                }

                // Migrate group settings from Default.ini
                if (File.Exists(defaultIniPath))
                {
                    MigrateGroupSettings("Default", defaultIniPath);
                }

                // Look for other profile INI files
                var profileIniFiles = Directory.GetFiles(Application.StartupPath, "*.ini")
                    .Where(f => !Path.GetFileName(f).Equals("progman64.ini", StringComparison.OrdinalIgnoreCase))
                    .Where(f => !Path.GetFileName(f).Equals("Default.ini", StringComparison.OrdinalIgnoreCase));

                foreach (var iniFile in profileIniFiles)
                {
                    var profileName = Path.GetFileNameWithoutExtension(iniFile);
                    MigrateGroupSettings(profileName, iniFile);
                }

                Save();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during INI migration: {ex.Message}", 
                    "Migration Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private static void MigrateApplicationSettings(string iniPath)
        {
            // This is a simplified migration - you could use the old INI reading methods
            // For now, we'll keep the defaults
        }

        private static void MigrateProfiles(string iniPath)
        {
            // This would read the [Profiles] section from progman64.ini
            // For now, we'll keep the default profile
        }

        private static void MigrateGroupSettings(string profileName, string iniPath)
        {
            // This would read group settings from the profile INI file
            // For now, we'll keep it simple
        }

        /// <summary>
        /// Get the current configuration instance
        /// </summary>
        public static ProgramConfig Instance => Load();
    }

    /// <summary>
    /// Main configuration structure
    /// </summary>
    public class ProgramConfig
    {
        public ApplicationSettings Application { get; set; } = new ApplicationSettings();
        public Dictionary<string, ProfileSettings> Profiles { get; set; } = new Dictionary<string, ProfileSettings>();
        public Dictionary<string, Dictionary<string, GroupSettings>> Groups { get; set; } = new Dictionary<string, Dictionary<string, GroupSettings>>();
    }

    /// <summary>
    /// Application-level settings
    /// </summary>
    public class ApplicationSettings
    {
        public int WindowWidth { get; set; } = 800;
        public int WindowHeight { get; set; } = 600;
        public int WindowX { get; set; } = 100;
        public int WindowY { get; set; } = 100;
        public int IconSize { get; set; } = 32;
        public int UsernameInTitle { get; set; } = 0;
        public string ActiveWindow { get; set; } = "";
        public string CurrentProfile { get; set; } = "Default";
    }

    /// <summary>
    /// Profile settings
    /// </summary>
    public class ProfileSettings
    {
        public string Path { get; set; } = "";
        public bool IsDefault { get; set; } = false;
    }

    /// <summary>
    /// Group/window settings
    /// </summary>
    public class GroupSettings
    {
        public int WindowStatus { get; set; } = 1; // 0=Minimized, 1=Normal, 2=Maximized
        public int X { get; set; } = 100;
        public int Y { get; set; } = 100;
        public int Width { get; set; } = 400;
        public int Height { get; set; } = 300;
        public List<string> ShortcutOrder { get; set; } = new List<string>();
    }
}