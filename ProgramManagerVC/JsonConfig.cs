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
                    System.Diagnostics.Debug.WriteLine($"JsonConfig.Load: ConfigFilePath = {ConfigFilePath}");

                    if (File.Exists(ConfigFilePath))
                    {
                        System.Diagnostics.Debug.WriteLine("JsonConfig.Load: Config file exists, reading...");
                        try
                        {
                            string json = File.ReadAllText(ConfigFilePath);
                            System.Diagnostics.Debug.WriteLine($"JsonConfig.Load: Read {json.Length} characters from JSON");

                            _config = SimpleJsonDeserializer.Deserialize(json);
                            System.Diagnostics.Debug.WriteLine($"JsonConfig.Load: Deserialized successfully");
                            System.Diagnostics.Debug.WriteLine($"  - WindowWidth: {_config.Application.WindowWidth}");
                            System.Diagnostics.Debug.WriteLine($"  - WindowHeight: {_config.Application.WindowHeight}");
                            System.Diagnostics.Debug.WriteLine($"  - CurrentProfile: {_config.Application.CurrentProfile}");
                            System.Diagnostics.Debug.WriteLine($"  - Profiles count: {_config.Profiles.Count}");
                            System.Diagnostics.Debug.WriteLine($"  - Groups count: {_config.Groups.Count}");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"JsonConfig.Load: Error deserializing JSON: {ex.Message}");
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
                        System.Diagnostics.Debug.WriteLine("JsonConfig.Load: Config file doesn't exist, creating default");
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
            var config = new ProgramConfig();

            try
            {
                // Parse JSON manually (simple parser for our specific structure)
                var lines = json.Split('\n').Select(l => l.Trim()).ToArray();

                bool inApplication = false;
                bool inProfiles = false;
                bool inGroups = false;
                bool inCurrentProfile = false;
                string currentProfileName = "";
                string currentGroupName = "";
                bool inCurrentGroup = false;

                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i];

                    if (line.Contains("\"application\":"))
                    {
                        inApplication = true;
                        inProfiles = false;
                        inGroups = false;
                    }
                    else if (line.Contains("\"profiles\":"))
                    {
                        inApplication = false;
                        inProfiles = true;
                        inGroups = false;
                    }
                    else if (line.Contains("\"groups\":"))
                    {
                        inApplication = false;
                        inProfiles = false;
                        inGroups = true;
                    }

                    if (inApplication)
                    {
                        ParseApplicationSetting(line, config.Application);
                    }
                    else if (inProfiles)
                    {
                        ParseProfileSettings(line, config, ref currentProfileName, ref inCurrentProfile);
                    }
                    else if (inGroups)
                    {
                        ParseGroupSettings(line, config, ref currentProfileName, ref currentGroupName, ref inCurrentProfile, ref inCurrentGroup);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"JSON parsing error: {ex.Message}");
                // Return default config if parsing fails
            }

            return config;
        }

        private static void ParseApplicationSetting(string line, ApplicationSettings app)
        {
            if (line.Contains("\"window_width\":"))
                app.WindowWidth = ExtractIntValue(line);
            else if (line.Contains("\"window_height\":"))
                app.WindowHeight = ExtractIntValue(line);
            else if (line.Contains("\"window_x\":"))
                app.WindowX = ExtractIntValue(line);
            else if (line.Contains("\"window_y\":"))
                app.WindowY = ExtractIntValue(line);
            else if (line.Contains("\"icon_size\":"))
                app.IconSize = ExtractIntValue(line);
            else if (line.Contains("\"username_in_title\":"))
                app.UsernameInTitle = ExtractIntValue(line);
            else if (line.Contains("\"active_window\":"))
                app.ActiveWindow = ExtractStringValue(line);
            else if (line.Contains("\"current_profile\":"))
                app.CurrentProfile = ExtractStringValue(line);
        }

        private static void ParseProfileSettings(string line, ProgramConfig config, ref string currentProfileName, ref bool inCurrentProfile)
        {
            if (line.Contains("\":") && line.Contains("{") && !line.Contains("\"profiles\":"))
            {
                currentProfileName = ExtractKey(line);
                inCurrentProfile = true;
                if (!config.Profiles.ContainsKey(currentProfileName))
                    config.Profiles[currentProfileName] = new ProfileSettings();
            }
            else if (inCurrentProfile && !string.IsNullOrEmpty(currentProfileName))
            {
                if (line.Contains("\"path\":"))
                    config.Profiles[currentProfileName].Path = ExtractStringValue(line);
                else if (line.Contains("\"is_default\":"))
                    config.Profiles[currentProfileName].IsDefault = ExtractBoolValue(line);
                else if (line.Contains("}"))
                    inCurrentProfile = false;
            }
        }

        private static void ParseGroupSettings(string line, ProgramConfig config, ref string currentProfileName, ref string currentGroupName, ref bool inCurrentProfile, ref bool inCurrentGroup)
        {
            if (line.Contains("\":") && line.Contains("{") && !line.Contains("\"groups\":"))
            {
                if (!inCurrentProfile)
                {
                    // This is a profile name under groups
                    currentProfileName = ExtractKey(line);
                    inCurrentProfile = true;
                    if (!config.Groups.ContainsKey(currentProfileName))
                        config.Groups[currentProfileName] = new Dictionary<string, GroupSettings>();
                }
                else
                {
                    // This is a group name under the current profile
                    currentGroupName = ExtractKey(line);
                    inCurrentGroup = true;
                    config.Groups[currentProfileName][currentGroupName] = new GroupSettings();
                }
            }
            else if (inCurrentGroup && !string.IsNullOrEmpty(currentGroupName))
            {
                var group = config.Groups[currentProfileName][currentGroupName];
                if (line.Contains("\"window_status\":"))
                    group.WindowStatus = ExtractIntValue(line);
                else if (line.Contains("\"x\":"))
                    group.X = ExtractIntValue(line);
                else if (line.Contains("\"y\":"))
                    group.Y = ExtractIntValue(line);
                else if (line.Contains("\"width\":"))
                    group.Width = ExtractIntValue(line);
                else if (line.Contains("\"height\":"))
                    group.Height = ExtractIntValue(line);
                else if (line.Contains("}") && !line.Contains("shortcut_order"))
                    inCurrentGroup = false;
            }
            else if (line.Contains("}") && inCurrentProfile && !inCurrentGroup)
            {
                inCurrentProfile = false;
            }
        }

        private static int ExtractIntValue(string line)
        {
            var colonIndex = line.IndexOf(':');
            if (colonIndex >= 0)
            {
                var valueStr = line.Substring(colonIndex + 1).Trim().TrimEnd(',');
                if (int.TryParse(valueStr, out int result))
                    return result;
            }
            return 0;
        }

        private static string ExtractStringValue(string line)
        {
            var colonIndex = line.IndexOf(':');
            if (colonIndex >= 0)
            {
                var valueStr = line.Substring(colonIndex + 1).Trim().TrimEnd(',');
                if (valueStr.StartsWith("\"") && valueStr.EndsWith("\""))
                    return valueStr.Substring(1, valueStr.Length - 2);
            }
            return "";
        }

        private static bool ExtractBoolValue(string line)
        {
            var colonIndex = line.IndexOf(':');
            if (colonIndex >= 0)
            {
                var valueStr = line.Substring(colonIndex + 1).Trim().TrimEnd(',');
                return valueStr == "true";
            }
            return false;
        }

        private static string ExtractKey(string line)
        {
            var colonIndex = line.IndexOf(':');
            if (colonIndex >= 0)
            {
                var keyStr = line.Substring(0, colonIndex).Trim();
                if (keyStr.StartsWith("\"") && keyStr.EndsWith("\""))
                    return keyStr.Substring(1, keyStr.Length - 2);
            }
            return "";
        }
    }
}