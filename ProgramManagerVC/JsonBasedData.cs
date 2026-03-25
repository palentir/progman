using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace ProgramManagerVC
{
    /// <summary>
    /// JSON-based data management class - replaces FileBasedData
    /// </summary>
    public static class JsonBasedData
    {
        private static ProgramConfig Config => JsonConfig.Instance;

        #region Application Settings

        /// <summary>
        /// Save an application setting
        /// </summary>
        public static void SaveApplicationSettings(string key, string value)
        {
            switch (key.ToLower())
            {
                case "window_width":
                    if (int.TryParse(value, out int width))
                        Config.Application.WindowWidth = width;
                    break;
                case "window_height":
                    if (int.TryParse(value, out int height))
                        Config.Application.WindowHeight = height;
                    break;
                case "window_x":
                    if (int.TryParse(value, out int x))
                        Config.Application.WindowX = x;
                    break;
                case "window_y":
                    if (int.TryParse(value, out int y))
                        Config.Application.WindowY = y;
                    break;
                case "icon_size":
                    if (int.TryParse(value, out int iconSize))
                        Config.Application.IconSize = iconSize;
                    break;
                case "username_in_title":
                    if (int.TryParse(value, out int usernameInTitle))
                        Config.Application.UsernameInTitle = usernameInTitle;
                    break;
                case "active_window":
                    Config.Application.ActiveWindow = value ?? "";
                    break;
            }
            
            JsonConfig.Save();
        }

        /// <summary>
        /// Load an application setting
        /// </summary>
        public static string LoadApplicationSetting(string key, string defaultValue = "")
        {
            switch (key.ToLower())
            {
                case "window_width":
                    return Config.Application.WindowWidth.ToString();
                case "window_height":
                    return Config.Application.WindowHeight.ToString();
                case "window_x":
                    return Config.Application.WindowX.ToString();
                case "window_y":
                    return Config.Application.WindowY.ToString();
                case "icon_size":
                    return Config.Application.IconSize.ToString();
                case "username_in_title":
                    return Config.Application.UsernameInTitle.ToString();
                case "active_window":
                    return Config.Application.ActiveWindow ?? "";
                default:
                    return defaultValue;
            }
        }

        #endregion

        #region Profile Management

        /// <summary>
        /// Get all profiles
        /// </summary>
        public static List<ProfileInfo> GetAllProfiles()
        {
            var profiles = new List<ProfileInfo>();
            
            foreach (var profile in Config.Profiles)
            {
                profiles.Add(new ProfileInfo
                {
                    Name = profile.Key,
                    Path = profile.Value.Path,
                    IsDefault = profile.Value.IsDefault
                });
            }

            return profiles;
        }

        /// <summary>
        /// Save a profile
        /// </summary>
        public static void SaveProfile(string name, string path)
        {
            Config.Profiles[name] = new ProfileSettings
            {
                Path = path,
                IsDefault = name == "Default"
            };
            
            // Ensure the profile has a groups section
            if (!Config.Groups.ContainsKey(name))
            {
                Config.Groups[name] = new Dictionary<string, GroupSettings>();
            }
            
            JsonConfig.Save();
        }

        /// <summary>
        /// Delete a profile
        /// </summary>
        public static void DeleteProfile(string name)
        {
            if (name == "Default") return; // Cannot delete Default profile

            Config.Profiles.Remove(name);
            Config.Groups.Remove(name);
            JsonConfig.Save();
        }

        /// <summary>
        /// Set current profile
        /// </summary>
        public static void SetCurrentProfile(string profileName)
        {
            Config.Application.CurrentProfile = profileName;
            JsonConfig.Save();
        }

        /// <summary>
        /// Get current profile
        /// </summary>
        public static string GetCurrentProfile()
        {
            return Config.Application.CurrentProfile ?? "Default";
        }

        #endregion

        #region Groups Management

        /// <summary>
        /// Get all groups for current profile
        /// </summary>
        public static List<GroupInfo> GetAllGroups()
        {
            var currentProfile = GetCurrentProfile();
            var groups = new List<GroupInfo>();
            var profilePath = GetGroupsFolder();

            System.Diagnostics.Debug.WriteLine($"GetAllGroups: Profile={currentProfile}, Path={profilePath}");

            if (!Directory.Exists(profilePath))
            {
                System.Diagnostics.Debug.WriteLine($"Profile path does not exist, creating: {profilePath}");
                Directory.CreateDirectory(profilePath);
                return groups;
            }

            // Always add "Programs" group (root folder)
            var programsGroup = new GroupInfo
            {
                Id = "Programs",
                Name = "Programs",
                FolderPath = profilePath,
                IconIndex = 0
            };

            // Load settings for Programs group from JSON
            LoadGroupSettings(programsGroup, currentProfile);
            groups.Add(programsGroup);
            System.Diagnostics.Debug.WriteLine($"Added Programs group");

            // Scan for subdirectories to create additional groups
            var subDirectories = Directory.GetDirectories(profilePath);
            System.Diagnostics.Debug.WriteLine($"Found {subDirectories.Length} subdirectories in {profilePath}");

            foreach (var subDir in subDirectories)
            {
                var groupName = Path.GetFileName(subDir);
                System.Diagnostics.Debug.WriteLine($"  Processing subdirectory: {groupName}");

                // Skip hidden directories and system directories
                if (groupName.StartsWith(".") || groupName.StartsWith("$"))
                {
                    System.Diagnostics.Debug.WriteLine($"  Skipping hidden/system directory: {groupName}");
                    continue;
                }

                var groupInfo = new GroupInfo
                {
                    Id = groupName,
                    Name = groupName,
                    FolderPath = subDir,
                    IconIndex = 0
                };

                // Load settings for this group from JSON
                LoadGroupSettings(groupInfo, currentProfile);
                groups.Add(groupInfo);
                System.Diagnostics.Debug.WriteLine($"  Added group: {groupName}");
            }

            System.Diagnostics.Debug.WriteLine($"GetAllGroups returning {groups.Count} groups total");
            return groups;
        }

        /// <summary>
        /// Load group settings from JSON
        /// </summary>
        private static void LoadGroupSettings(GroupInfo groupInfo, string profileName)
        {
            if (!Config.Groups.ContainsKey(profileName))
            {
                Config.Groups[profileName] = new Dictionary<string, GroupSettings>();
            }

            System.Diagnostics.Debug.WriteLine($"LoadGroupSettings: Group='{groupInfo.Name}', Profile='{profileName}'");

            if (Config.Groups[profileName].ContainsKey(groupInfo.Name))
            {
                var settings = Config.Groups[profileName][groupInfo.Name];
                groupInfo.WindowStatus = settings.WindowStatus;
                groupInfo.X = settings.X;
                groupInfo.Y = settings.Y;
                groupInfo.Width = settings.Width;
                groupInfo.Height = settings.Height;

                System.Diagnostics.Debug.WriteLine($"  Loaded from JSON: Status={settings.WindowStatus}, X={settings.X}, Y={settings.Y}, W={settings.Width}, H={settings.Height}");
            }
            else
            {
                // Default values
                groupInfo.WindowStatus = 1; // Normal
                groupInfo.X = 100;
                groupInfo.Y = 100;
                groupInfo.Width = 400;
                groupInfo.Height = 300;

                System.Diagnostics.Debug.WriteLine($"  Using defaults: Status=1, X=100, Y=100, W=400, H=300");
            }
        }

        /// <summary>
        /// Save group settings to JSON
        /// </summary>
        public static void SaveGroupSettings(GroupInfo groupInfo)
        {
            var currentProfile = GetCurrentProfile();

            System.Diagnostics.Debug.WriteLine($"SaveGroupSettings: Group='{groupInfo.Name}', Profile='{currentProfile}'");
            System.Diagnostics.Debug.WriteLine($"  Values: Status={groupInfo.WindowStatus}, X={groupInfo.X}, Y={groupInfo.Y}, W={groupInfo.Width}, H={groupInfo.Height}");

            if (!Config.Groups.ContainsKey(currentProfile))
            {
                Config.Groups[currentProfile] = new Dictionary<string, GroupSettings>();
            }

            Config.Groups[currentProfile][groupInfo.Name] = new GroupSettings
            {
                WindowStatus = groupInfo.WindowStatus,
                X = groupInfo.X,
                Y = groupInfo.Y,
                Width = groupInfo.Width,
                Height = groupInfo.Height,
                ShortcutOrder = Config.Groups[currentProfile].ContainsKey(groupInfo.Name) ?
                    Config.Groups[currentProfile][groupInfo.Name].ShortcutOrder :
                    new List<string>()
            };

            JsonConfig.Save();
            System.Diagnostics.Debug.WriteLine($"  Saved to JSON successfully");
        }

        /// <summary>
        /// Delete a group
        /// </summary>
        public static void DeleteGroup(string groupName)
        {
            if (groupName == "Programs") return; // Cannot delete Programs group

            // Remove from JSON
            var currentProfile = GetCurrentProfile();
            if (Config.Groups.ContainsKey(currentProfile))
            {
                Config.Groups[currentProfile].Remove(groupName);
                JsonConfig.Save();
            }

            // Remove the folder
            var groupPath = GetGroupFolderPath(groupName);
            if (Directory.Exists(groupPath))
            {
                try
                {
                    Directory.Delete(groupPath, true);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting group folder: {ex.Message}",
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        /// <summary>
        /// Create a new group
        /// </summary>
        public static void CreateGroup(string groupName, string groupPath = null)
        {
            var currentProfile = GetCurrentProfile();
            
            // Create the folder if it doesn't exist
            if (string.IsNullOrEmpty(groupPath))
            {
                groupPath = GetGroupFolderPath(groupName);
            }
            
            if (!Directory.Exists(groupPath))
            {
                Directory.CreateDirectory(groupPath);
            }

            // Add to JSON config
            if (!Config.Groups.ContainsKey(currentProfile))
            {
                Config.Groups[currentProfile] = new Dictionary<string, GroupSettings>();
            }

            if (!Config.Groups[currentProfile].ContainsKey(groupName))
            {
                Config.Groups[currentProfile][groupName] = new GroupSettings
                {
                    WindowStatus = 1, // Normal
                    X = 100,
                    Y = 100,
                    Width = 400,
                    Height = 300,
                    ShortcutOrder = new List<string>()
                };
                
                JsonConfig.Save();
            }
        }

        private static string GetGroupFolderPath(string groupName)
        {
            var profilePath = GetGroupsFolder();
            
            if (groupName == "Programs")
            {
                return profilePath; // Programs group uses the root folder
            }
            else
            {
                return Path.Combine(profilePath, groupName);
            }
        }

        #endregion

        #region Shortcut Order Management

        /// <summary>
        /// Save shortcut display order
        /// </summary>
        public static void SaveShortcutDisplayOrder(List<ShortcutInfo> shortcuts, string groupName)
        {
            var currentProfile = GetCurrentProfile();
            
            if (!Config.Groups.ContainsKey(currentProfile))
            {
                Config.Groups[currentProfile] = new Dictionary<string, GroupSettings>();
            }

            if (!Config.Groups[currentProfile].ContainsKey(groupName))
            {
                Config.Groups[currentProfile][groupName] = new GroupSettings();
            }

            Config.Groups[currentProfile][groupName].ShortcutOrder = shortcuts.Select(s => s.Name).ToList();
            JsonConfig.Save();
        }

        /// <summary>
        /// Get shortcut display order
        /// </summary>
        public static List<string> GetShortcutDisplayOrder(string groupName)
        {
            var currentProfile = GetCurrentProfile();
            
            if (Config.Groups.ContainsKey(currentProfile) &&
                Config.Groups[currentProfile].ContainsKey(groupName))
            {
                return Config.Groups[currentProfile][groupName].ShortcutOrder ?? new List<string>();
            }

            return new List<string>();
        }

        #endregion

        #region FileBasedData Compatibility Methods
        
        private static string currentGroupsFolder = "";

        public static void SetGroupsFolder(string folderPath)
        {
            currentGroupsFolder = folderPath;
        }

        public static string GetGroupsFolder()
        {
            if (!string.IsNullOrEmpty(currentGroupsFolder))
            {
                System.Diagnostics.Debug.WriteLine($"Using currentGroupsFolder: {currentGroupsFolder}");
                return currentGroupsFolder;
            }

            var currentProfile = GetCurrentProfile();
            System.Diagnostics.Debug.WriteLine($"Current profile: {currentProfile}");

            if (Config.Profiles.ContainsKey(currentProfile))
            {
                var profilePath = Config.Profiles[currentProfile].Path;
                System.Diagnostics.Debug.WriteLine($"Profile path from config: {profilePath}");

                // MIGRATION FIX: If the path ends with "Groups", change it to "Shortcuts"
                if (profilePath.EndsWith("Groups"))
                {
                    profilePath = profilePath.Replace("Groups", "Shortcuts");
                    System.Diagnostics.Debug.WriteLine($"Migrated path from Groups to Shortcuts: {profilePath}");

                    // Update the profile with the correct path
                    Config.Profiles[currentProfile].Path = profilePath;
                    JsonConfig.Save();
                    System.Diagnostics.Debug.WriteLine($"Updated profile path in JSON config");
                }

                return profilePath;
            }

            var defaultPath = Path.Combine(Application.StartupPath, "Shortcuts");
            System.Diagnostics.Debug.WriteLine($"Using default path: {defaultPath}");
            return defaultPath;
        }

        /// <summary>
        /// Get shortcuts in a group with root handling for Programs group
        /// </summary>
        public static List<ShortcutInfo> GetShortcutsInGroupWithRoot(string groupName)
        {
            return FileBasedData.GetShortcutsInGroupWithRoot(groupName);
        }

        // Forward to existing FileBasedData methods for now (shortcuts creation/deletion)
        public static void CreateShortcut(string groupName, string name, string targetPath, string arguments, string iconPath, int iconIndex)
        {
            FileBasedData.CreateShortcut(groupName, name, targetPath, arguments, iconPath, iconIndex);
        }

        public static void DeleteShortcut(string groupName, string shortcutFileName)
        {
            FileBasedData.DeleteShortcut(groupName, shortcutFileName);
        }

        public static List<ShortcutInfo> GetShortcutsInGroup(string groupName)
        {
            return FileBasedData.GetShortcutsInGroup(groupName);
        }

        /// <summary>
        /// Expand environment variables in a path
        /// </summary>
        public static string ExpandEnvironmentVariables(string path)
        {
            return Environment.ExpandEnvironmentVariables(path);
        }

        /// <summary>
        /// Check if a string contains environment variables
        /// </summary>
        public static bool ContainsEnvironmentVariables(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            return text.Contains("%");
        }

        /// <summary>
        /// Enhanced environment variable expansion
        /// </summary>
        public static string ExpandEnvironmentVariablesEnhanced(string path)
        {
            if (string.IsNullOrEmpty(path)) return path;

            // First do standard expansion
            string result = Environment.ExpandEnvironmentVariables(path);

            // Handle additional common variables
            result = result.Replace("%USERNAME%", Environment.UserName);
            result = result.Replace("%COMPUTERNAME%", Environment.MachineName);

            return result;
        }

        #endregion
    }
}