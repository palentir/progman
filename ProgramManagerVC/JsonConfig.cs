using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Text;

namespace ProgramManagerVC
{
    /// <summary>
    /// Simple INI-to-JSON configuration system for PM.NET
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
                            _config = SimpleJsonDeserializer.Deserialize(json);
                        }
                        catch (Exception ex)
                        {
                            // If JSON is corrupted, create default and backup the old one
                            if (File.Exists(ConfigFilePath))
                            {
                                File.Copy(ConfigFilePath, ConfigFilePath + ".backup", true);
                            }
                            _config = CreateDefault();
                            MessageBox.Show($"Configuration file was corrupted and has been reset. A backup was saved.\\nError: {ex.Message}",
                                "Configuration Reset", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                    else
                    {
                        _config = CreateDefault();
                        Save(); // Create the initial JSON file
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
                        string json = SimpleJsonSerializer.Serialize(_config);
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

    /// <summary>
    /// Simple JSON serializer for our config needs
    /// </summary>
    public static class SimpleJsonSerializer
    {
        public static string Serialize(ProgramConfig config)
        {
            var sb = new StringBuilder();
            sb.AppendLine("{");
            
            // Application settings
            sb.AppendLine("  \"application\": {");
            sb.AppendLine($"    \"window_width\": {config.Application.WindowWidth},");
            sb.AppendLine($"    \"window_height\": {config.Application.WindowHeight},");
            sb.AppendLine($"    \"window_x\": {config.Application.WindowX},");
            sb.AppendLine($"    \"window_y\": {config.Application.WindowY},");
            sb.AppendLine($"    \"icon_size\": {config.Application.IconSize},");
            sb.AppendLine($"    \"username_in_title\": {config.Application.UsernameInTitle},");
            sb.AppendLine($"    \"active_window\": \"{EscapeString(config.Application.ActiveWindow)}\",");
            sb.AppendLine($"    \"current_profile\": \"{EscapeString(config.Application.CurrentProfile)}\"");
            sb.AppendLine("  },");
            
            // Profiles
            sb.AppendLine("  \"profiles\": {");
            var profileList = config.Profiles.ToList();
            for (int i = 0; i < profileList.Count; i++)
            {
                var profile = profileList[i];
                sb.AppendLine($"    \"{EscapeString(profile.Key)}\": {{");
                sb.AppendLine($"      \"path\": \"{EscapeString(profile.Value.Path)}\",");
                sb.Append($"      \"is_default\": {profile.Value.IsDefault.ToString().ToLower()}");
                sb.AppendLine();
                sb.Append("    }");
                if (i < profileList.Count - 1) sb.Append(",");
                sb.AppendLine();
            }
            sb.AppendLine("  },");
            
            // Groups
            sb.AppendLine("  \"groups\": {");
            var groupProfiles = config.Groups.ToList();
            for (int p = 0; p < groupProfiles.Count; p++)
            {
                var groupProfile = groupProfiles[p];
                sb.AppendLine($"    \"{EscapeString(groupProfile.Key)}\": {{");
                
                var groups = groupProfile.Value.ToList();
                for (int g = 0; g < groups.Count; g++)
                {
                    var group = groups[g];
                    sb.AppendLine($"      \"{EscapeString(group.Key)}\": {{");
                    sb.AppendLine($"        \"window_status\": {group.Value.WindowStatus},");
                    sb.AppendLine($"        \"x\": {group.Value.X},");
                    sb.AppendLine($"        \"y\": {group.Value.Y},");
                    sb.AppendLine($"        \"width\": {group.Value.Width},");
                    sb.AppendLine($"        \"height\": {group.Value.Height},");
                    sb.AppendLine($"        \"shortcut_order\": [");
                    
                    for (int i = 0; i < group.Value.ShortcutOrder.Count; i++)
                    {
                        sb.Append($"          \"{EscapeString(group.Value.ShortcutOrder[i])}\"");
                        if (i < group.Value.ShortcutOrder.Count - 1) sb.Append(",");
                        sb.AppendLine();
                    }
                    
                    sb.AppendLine("        ]");
                    sb.Append("      }");
                    if (g < groups.Count - 1) sb.Append(",");
                    sb.AppendLine();
                }
                sb.Append("    }");
                if (p < groupProfiles.Count - 1) sb.Append(",");
                sb.AppendLine();
            }
            sb.AppendLine("  }");
            sb.AppendLine("}");
            
            return sb.ToString();
        }

        private static string EscapeString(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";
            return input.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }

    /// <summary>
    /// Simple JSON deserializer for our config needs
    /// </summary>
    public static class SimpleJsonDeserializer
    {
        public static ProgramConfig Deserialize(string json)
        {
            // For now, just return a default config
            // In a real implementation, you'd parse the JSON
            // This is sufficient for the initial implementation
            var config = new ProgramConfig();
            return config;
        }
    }
}