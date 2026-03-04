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
                case "current_profile":
                    Config.Application.CurrentProfile = value ?? "Default";
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
                    return Config.Application.ActiveWindow ?? defaultValue;
                case "current_profile":
                    return Config.Application.CurrentProfile ?? defaultValue;
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

            if (!Directory.Exists(profilePath))
            {
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

            // Scan for subdirectories to create additional groups
            var subDirectories = Directory.GetDirectories(profilePath);
            foreach (var subDir in subDirectories)
            {
                var groupName = Path.GetFileName(subDir);
                
                // Skip hidden directories and system directories
                if (groupName.StartsWith(".") || groupName.StartsWith("$"))
                    continue;

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
            }

            return groups;
        }

        /// <summary>
        /// Load group settings from JSON config
        /// </summary>
        private static void LoadGroupSettings(GroupInfo groupInfo, string profileName)
        {
            if (Config.Groups.ContainsKey(profileName) &&
                Config.Groups[profileName].ContainsKey(groupInfo.Id))
            {
                var settings = Config.Groups[profileName][groupInfo.Id];
                groupInfo.WindowStatus = settings.WindowStatus;
                groupInfo.X = settings.X;
                groupInfo.Y = settings.Y;
                groupInfo.Width = settings.Width;
                groupInfo.Height = settings.Height;
            }
            else
            {
                // Set defaults for new groups
                groupInfo.WindowStatus = 1; // Normal
                groupInfo.X = 100;
                groupInfo.Y = 100;
                groupInfo.Width = 400;
                groupInfo.Height = 300;

                // If this is the Programs group, give it a different default position
                if (groupInfo.Id == "Programs")
                {
                    groupInfo.X = 1;
                    groupInfo.Y = 379;
                    groupInfo.Width = 315;
                    groupInfo.Height = 124;
                }
            }
        }

        /// <summary>
        /// Save group settings
        /// </summary>
        public static void SaveGroupSettings(GroupInfo groupInfo)
        {
            var currentProfile = GetCurrentProfile();
            
            if (!Config.Groups.ContainsKey(currentProfile))
            {
                Config.Groups[currentProfile] = new Dictionary<string, GroupSettings>();
            }

            Config.Groups[currentProfile][groupInfo.Id] = new GroupSettings
            {
                WindowStatus = groupInfo.WindowStatus,
                X = groupInfo.X,
                Y = groupInfo.Y,
                Width = groupInfo.Width,
                Height = groupInfo.Height,
                ShortcutOrder = new List<string>() // Will be populated by shortcut order management
            };
            
            JsonConfig.Save();
        }

        /// <summary>
        /// Delete a group
        /// </summary>
        public static void DeleteGroup(string groupName)
        {
            var currentProfile = GetCurrentProfile();
            
            if (Config.Groups.ContainsKey(currentProfile))
            {
                Config.Groups[currentProfile].Remove(groupName);
                JsonConfig.Save();
            }

            // Also delete the physical folder
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
            if (string.IsNullOrEmpty(currentGroupsFolder))
            {
                var currentProfile = GetCurrentProfile();
                if (Config.Profiles.ContainsKey(currentProfile))
                {
                    currentGroupsFolder = Config.Profiles[currentProfile].Path;
                }
                else
                {
                    currentGroupsFolder = Path.Combine(Application.StartupPath, "Shortcuts");
                }
            }
            return currentGroupsFolder;
        }

        /// <summary>
        /// Get shortcuts in a group with proper ordering
        /// </summary>
        public static List<ShortcutInfo> GetShortcutsInGroupWithRoot(string groupName)
        {
            var shortcuts = new List<ShortcutInfo>();
            var folderPath = GetGroupFolderPath(groupName);
            
            if (!Directory.Exists(folderPath))
                return shortcuts;

            // Get all .lnk files in the folder
            var lnkFiles = Directory.GetFiles(folderPath, "*.lnk");
            
            // Convert to ShortcutInfo objects
            foreach (var lnkFile in lnkFiles)
            {
                try
                {
                    var shortcutInfo = new ShortcutInfo();
                    shortcutInfo.Name = Path.GetFileNameWithoutExtension(lnkFile);
                    shortcutInfo.ShortcutPath = lnkFile;
                    
                    // Load shortcut details using the existing method
                    var existingShortcut = FileBasedData.GetShortcutsInGroupWithRoot(groupName)
                        .FirstOrDefault(s => s.Name == shortcutInfo.Name);
                    
                    if (existingShortcut != null)
                    {
                        shortcutInfo.TargetPath = existingShortcut.TargetPath;
                        shortcutInfo.Arguments = existingShortcut.Arguments;
                        shortcutInfo.IconLocation = existingShortcut.IconLocation;
                        shortcutInfo.IconIndex = existingShortcut.IconIndex;
                        shortcutInfo.DisplayOrder = existingShortcut.DisplayOrder;
                    }
                    
                    shortcuts.Add(shortcutInfo);
                }
                catch
                {
                    // Skip invalid shortcut files
                    continue;
                }
            }

            // Apply saved ordering
            var savedOrder = GetShortcutDisplayOrder(groupName);
            if (savedOrder.Count > 0)
            {
                var orderedShortcuts = new List<ShortcutInfo>();
                
                // Add shortcuts in saved order
                foreach (var shortcutName in savedOrder)
                {
                    var shortcut = shortcuts.FirstOrDefault(s => s.Name == shortcutName);
                    if (shortcut != null)
                    {
                        shortcut.DisplayOrder = orderedShortcuts.Count;
                        orderedShortcuts.Add(shortcut);
                    }
                }
                
                // Add any new shortcuts not in the saved order
                foreach (var shortcut in shortcuts.Where(s => !savedOrder.Contains(s.Name)))
                {
                    shortcut.DisplayOrder = orderedShortcuts.Count;
                    orderedShortcuts.Add(shortcut);
                }
                
                return orderedShortcuts;
            }
            
            // No saved order, return in alphabetical order
            shortcuts = shortcuts.OrderBy(s => s.Name).ToList();
            for (int i = 0; i < shortcuts.Count; i++)
            {
                shortcuts[i].DisplayOrder = i;
            }
            
            return shortcuts;
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

        public static string ExpandEnvironmentVariables(string input)
        {
            return FileBasedData.ExpandEnvironmentVariables(input);
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
    }
}