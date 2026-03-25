using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;

namespace ProgramManagerVC
{
    /// <summary>
    /// Handles file-based data management using folders, .lnk files, and .ini files
    /// </summary>
    public static class FileBasedData
    {
        private static string currentGroupsFolder = Path.Combine(Application.StartupPath, "Shortcuts");
        
        /// <summary>
        /// Sets the groups folder to a custom location
        /// </summary>
        public static void SetGroupsFolder(string folderPath)
        {
            currentGroupsFolder = folderPath;
        }
        
        /// <summary>
        /// Gets the current groups folder path
        /// </summary>
        public static string GetGroupsFolder()
        {
            return currentGroupsFolder;
        }

        #region Group Management

        /// <summary>
        /// Gets all group folders
        /// </summary>
        public static List<GroupInfo> GetAllGroups()
        {
            return GetAllGroupsWithRootHandling();
        }

        /// <summary>
        /// Gets all groups with root handling, using profile INI files for storage
        /// </summary>
        public static List<GroupInfo> GetAllGroupsWithRootHandling()
        {
            var groups = new List<GroupInfo>();
            
            if (!Directory.Exists(currentGroupsFolder))
                Directory.CreateDirectory(currentGroupsFolder);

            // First, try to load from profile INI
            var profileGroups = GetGroupsFromProfileIni();
            
            // Check for .lnk files in the root of the groups folder OR if Programs subfolder exists
            var rootLnkFiles = Directory.GetFiles(currentGroupsFolder, "*.lnk", SearchOption.TopDirectoryOnly);
            var programsSubfolder = Path.Combine(currentGroupsFolder, "Programs");
            bool programsFolderExists = Directory.Exists(programsSubfolder);
            
            if (rootLnkFiles.Length > 0 || programsFolderExists)
            {
                // Check if Programs group already exists in profile
                var programsGroup = profileGroups.FirstOrDefault(g => g.Name == "Programs");
                if (programsGroup == null)
                {
                    // Create a virtual "Programs" group for root .lnk files
                    programsGroup = new GroupInfo
                    {
                        Id = "0",
                        Name = "Programs",
                        FolderPath = currentGroupsFolder,
                        WindowStatus = 1,
                        X = 100,
                        Y = 100,
                        Width = 400,
                        Height = 300,
                        IconIndex = 0
                    };
                    
                    // Save to profile INI
                    SaveGroupSettings(programsGroup);
                }
                groups.Add(programsGroup);
            }
            else
            {
                // Still add Programs group from profile if it exists
                var programsGroup = profileGroups.FirstOrDefault(g => g.Name == "Programs");
                if (programsGroup != null)
                {
                    groups.Add(programsGroup);
                }
            }

            // Get subfolder groups and ensure they're in the profile INI (exclude "Programs" subfolder)
            var directories = Directory.GetDirectories(currentGroupsFolder)
                .Where(d => !string.Equals(Path.GetFileName(d), "Programs", StringComparison.OrdinalIgnoreCase));
            
            foreach (var directory in directories)
            {
                var dirInfo = new DirectoryInfo(directory);
                var existingGroup = profileGroups.FirstOrDefault(g => 
                    string.Equals(g.Name, dirInfo.Name, StringComparison.OrdinalIgnoreCase) && 
                    g.Name != "Programs");
                
                if (existingGroup != null)
                {
                    // Use existing group from profile INI
                    if (!groups.Any(g => string.Equals(g.Name, existingGroup.Name, StringComparison.OrdinalIgnoreCase)))
                    {
                        groups.Add(existingGroup);
                    }
                }
                else
                {
                    // Create new group entry - SET TO MINIMIZED BY DEFAULT
                    var newGroup = new GroupInfo
                    {
                        Id = groups.Count.ToString(),
                        Name = dirInfo.Name,
                        FolderPath = directory,
                        WindowStatus = 0, // 0 = Minimized by default
                        X = 100 + (groups.Count * 20), // Cascade windows
                        Y = 100 + (groups.Count * 20),
                        Width = 400,
                        Height = 300,
                        IconIndex = groups.Count
                    };
                    
                    // Save new group to profile INI
                    SaveGroupSettings(newGroup);
                    groups.Add(newGroup);
                }
            }

            // Update IDs to be sequential
            for (int i = 0; i < groups.Count; i++)
            {
                groups[i].Id = i.ToString();
                groups[i].IconIndex = i;
            }

            return groups;
        }

        /// <summary>
        /// Creates a new group folder and adds it to the profile INI
        /// </summary>
        public static void CreateGroup(string groupName)
        {
            var groupPath = Path.Combine(currentGroupsFolder, groupName);
            if (!Directory.Exists(groupPath))
            {
                Directory.CreateDirectory(groupPath);
            }
            
            // Create group entry in profile INI
            var currentProfile = GetCurrentProfile();
            var profileIniPath = Path.Combine(Application.StartupPath, currentProfile + ".ini");
            var sectionName = $"Group_{groupName}";
            
            // Only add if it doesn't already exist
            if (string.IsNullOrEmpty(ReadIniString(profileIniPath, sectionName, "Name", "")))
            {
                WriteIniValue(profileIniPath, sectionName, "Name", groupName);
                WriteIniValue(profileIniPath, sectionName, "Status", "0"); // 0 = Minimized by default
                WriteIniValue(profileIniPath, sectionName, "X", "100");
                WriteIniValue(profileIniPath, sectionName, "Y", "100");
                WriteIniValue(profileIniPath, sectionName, "Width", "400");
                WriteIniValue(profileIniPath, sectionName, "Height", "300");
            }
        }

        /// <summary>
        /// Deletes a group folder and removes it from the profile INI
        /// </summary>
        public static void DeleteGroup(string groupName)
        {
            var groupPath = Path.Combine(currentGroupsFolder, groupName);
            if (Directory.Exists(groupPath))
            {
                Directory.Delete(groupPath, true);
            }
            
            // Remove group entry from profile INI
            var currentProfile = GetCurrentProfile();
            var profileIniPath = Path.Combine(Application.StartupPath, currentProfile + ".ini");
            var sectionName = $"Group_{groupName}";
            
            // Clear all values in the section (effectively deletes it)
            WriteIniValue(profileIniPath, sectionName, "Name", "");
            WriteIniValue(profileIniPath, sectionName, "Status", "");
            WriteIniValue(profileIniPath, sectionName, "X", "");
            WriteIniValue(profileIniPath, sectionName, "Y", "");
            WriteIniValue(profileIniPath, sectionName, "Width", "");
            WriteIniValue(profileIniPath, sectionName, "Height", "");
        }

        /// <summary>
        /// Renames a group folder and updates the profile INI
        /// </summary>
        public static void RenameGroup(string oldName, string newName)
        {
            var oldPath = Path.Combine(currentGroupsFolder, oldName);
            var newPath = Path.Combine(currentGroupsFolder, newName);
            
            if (Directory.Exists(oldPath) && !Directory.Exists(newPath))
            {
                Directory.Move(oldPath, newPath);
                
                // Update profile INI
                var currentProfile = GetCurrentProfile();
                var profileIniPath = Path.Combine(Application.StartupPath, currentProfile + ".ini");
                var oldSectionName = $"Group_{oldName}";
                var newSectionName = $"Group_{newName}";
                
                // Copy settings to new section
                var status = ReadIniString(profileIniPath, oldSectionName, "Status", "1");
                var x = ReadIniString(profileIniPath, oldSectionName, "X", "100");
                var y = ReadIniString(profileIniPath, oldSectionName, "Y", "100");
                var width = ReadIniString(profileIniPath, oldSectionName, "Width", "400");
                var height = ReadIniString(profileIniPath, oldSectionName, "Height", "300");
                
                WriteIniValue(profileIniPath, newSectionName, "Name", newName);
                WriteIniValue(profileIniPath, newSectionName, "Status", status);
                WriteIniValue(profileIniPath, newSectionName, "X", x);
                WriteIniValue(profileIniPath, newSectionName, "Y", y);
                WriteIniValue(profileIniPath, newSectionName, "Width", width);
                WriteIniValue(profileIniPath, newSectionName, "Height", height);
                
                // Clear old section
                WriteIniValue(profileIniPath, oldSectionName, "Name", "");
                WriteIniValue(profileIniPath, oldSectionName, "Status", "");
                WriteIniValue(profileIniPath, oldSectionName, "X", "");
                WriteIniValue(profileIniPath, oldSectionName, "Y", "");
                WriteIniValue(profileIniPath, oldSectionName, "Width", "");
                WriteIniValue(profileIniPath, oldSectionName, "Height", "");
            }
        }

        #endregion

        #region Shortcut Management

        /// <summary>
        /// Gets all shortcuts in a group folder (including subfolders)
        /// </summary>
        public static List<ShortcutInfo> GetShortcutsInGroup(string groupName)
        {
            var shortcuts = new List<ShortcutInfo>();
            var groupPath = Path.Combine(currentGroupsFolder, groupName);
            
            if (!Directory.Exists(groupPath))
                return shortcuts;

            // Search recursively for .lnk files in the group folder and all subfolders
            var lnkFiles = Directory.GetFiles(groupPath, "*.lnk", SearchOption.AllDirectories);
            
            System.Diagnostics.Debug.WriteLine($"Found {lnkFiles.Length} shortcut files in group '{groupName}' (including subfolders)");
            
            foreach (var lnkFile in lnkFiles)
            {
                try
                {
                    var shortcutInfo = ReadShortcutFile(lnkFile);
                    if (shortcutInfo != null && !string.IsNullOrEmpty(shortcutInfo.TargetPath))
                    {
                        // Add info about subfolder location if not in root
                        var relativePath = Path.GetDirectoryName(lnkFile).Replace(groupPath, "").TrimStart('\\');
                        if (!string.IsNullOrEmpty(relativePath))
                        {
                            shortcutInfo.Description += $" (from subfolder: {relativePath})";
                        }
                        
                        shortcuts.Add(shortcutInfo);
                        System.Diagnostics.Debug.WriteLine($"Loaded shortcut: {shortcutInfo.Name} -> {shortcutInfo.TargetPath}");
                    }
                }
                catch (Exception ex)
                {
                    // Log error but continue with other shortcuts
                    System.Diagnostics.Debug.WriteLine($"Error reading shortcut {lnkFile}: {ex.Message}");
                    
                    // Create a basic shortcut info for files that can't be read
                    var fallbackInfo = new ShortcutInfo
                    {
                        Name = Path.GetFileNameWithoutExtension(lnkFile),
                        ShortcutPath = lnkFile,
                        TargetPath = lnkFile, // Use the shortcut path as fallback
                        Arguments = "",
                        WorkingDirectory = Path.GetDirectoryName(lnkFile),
                        Description = "Shortcut file (could not read properties)",
                        IconLocation = lnkFile,
                        IconIndex = 0
                    };
                    shortcuts.Add(fallbackInfo);
                }
            }

            System.Diagnostics.Debug.WriteLine($"Successfully loaded {shortcuts.Count} shortcuts for group '{groupName}'");

            // Load display order from INI and apply it
            LoadShortcutDisplayOrder(shortcuts, groupName);

            return shortcuts;
        }

        /// <summary>
        /// Gets all shortcuts in a group folder, including root folder handling
        /// </summary>
        public static List<ShortcutInfo> GetShortcutsInGroupWithRoot(string groupName)
        {
            var shortcuts = new List<ShortcutInfo>();

            // Always check for subfolders with the group name first (for backward compatibility)
            var groupPath = Path.Combine(currentGroupsFolder, groupName);
            if (Directory.Exists(groupPath))
            {
                var subFolderShortcuts = GetShortcutsInGroup(groupName);
                shortcuts.AddRange(subFolderShortcuts);
            }

            // For "Programs" group, also look for .lnk files in the root folder
            if (groupName == "Programs")
            {
                var rootLnkFiles = Directory.GetFiles(currentGroupsFolder, "*.lnk", SearchOption.TopDirectoryOnly);
                foreach (var lnkFile in rootLnkFiles)
                {
                    try
                    {
                        var shortcutInfo = ReadShortcutFile(lnkFile);
                        if (shortcutInfo != null && !string.IsNullOrEmpty(shortcutInfo.TargetPath))
                        {
                            shortcuts.Add(shortcutInfo);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error reading root shortcut {lnkFile}: {ex.Message}");
                    }
                }
            }

            // Load display order from INI and apply it
            LoadShortcutDisplayOrder(shortcuts, groupName);

            return shortcuts;
        }

        /// <summary>
        /// Creates a new shortcut file in the specified group
        /// </summary>
        public static void CreateShortcut(string groupName, string shortcutName, string targetPath, string arguments = "", string iconPath = "", int iconIndex = 0)
        {
            string shortcutPath;

            if (groupName == "Programs")
            {
                // For Programs group, place shortcuts directly in the root folder
                shortcutPath = Path.Combine(currentGroupsFolder, shortcutName + ".lnk");
            }
            else
            {
                // For other groups, create a subfolder
                var groupPath = Path.Combine(currentGroupsFolder, groupName);
                if (!Directory.Exists(groupPath))
                    CreateGroup(groupName);

                shortcutPath = Path.Combine(groupPath, shortcutName + ".lnk");
            }

            // Create the shortcut using IShellLink
            CreateShortcutFile(shortcutPath, targetPath, arguments, iconPath, iconIndex);
        }

        /// <summary>
        /// Deletes a shortcut file
        /// </summary>
        public static void DeleteShortcut(string groupName, string shortcutFileName)
        {
            string shortcutPath;

            if (groupName == "Programs")
            {
                // For Programs group, shortcuts are in the root folder
                shortcutPath = Path.Combine(currentGroupsFolder, shortcutFileName);
            }
            else
            {
                // For other groups, shortcuts are in subfolders
                var groupPath = Path.Combine(currentGroupsFolder, groupName);
                shortcutPath = Path.Combine(groupPath, shortcutFileName);
            }

            if (File.Exists(shortcutPath))
            {
                File.Delete(shortcutPath);
            }
        }

        #endregion

        #region Shortcut Display Order Management

        /// <summary>
        /// Loads and applies display order from INI file
        /// </summary>
        private static void LoadShortcutDisplayOrder(List<ShortcutInfo> shortcuts, string groupName)
        {
            var currentProfile = GetCurrentProfile();
            var profileIniPath = Path.Combine(Application.StartupPath, currentProfile + ".ini");

            if (!File.Exists(profileIniPath))
                return;

            var sectionName = groupName == "Programs" ? "ShortcutOrder_Programs" : $"ShortcutOrder_{groupName}";

            for (int i = 0; i < shortcuts.Count; i++)
            {
                var keyName = $"Shortcut_{i}";
                var savedName = ReadIniString(profileIniPath, sectionName, keyName, "");

                if (!string.IsNullOrEmpty(savedName))
                {
                    // Find shortcut by name and assign display order
                    var foundShortcut = shortcuts.FirstOrDefault(s => s.Name == savedName);
                    if (foundShortcut != null)
                    {
                        foundShortcut.DisplayOrder = i;
                    }
                }
            }

            // Sort by display order, then by name for any without order
            shortcuts.Sort((a, b) => 
            {
                if (a.DisplayOrder != b.DisplayOrder)
                    return a.DisplayOrder.CompareTo(b.DisplayOrder);
                return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
            });
        }

        /// <summary>
        /// Saves shortcut display order to INI file
        /// </summary>
        public static void SaveShortcutDisplayOrder(List<ShortcutInfo> shortcuts, string groupName)
        {
            var currentProfile = GetCurrentProfile();
            var profileIniPath = Path.Combine(Application.StartupPath, currentProfile + ".ini");

            var sectionName = groupName == "Programs" ? "ShortcutOrder_Programs" : $"ShortcutOrder_{groupName}";

            // Clear existing entries
            var allSections = GetAllIniSections(profileIniPath);
            if (allSections.Contains(sectionName))
            {
                // Remove the entire section first
                WritePrivateProfileString(sectionName, null, null, profileIniPath);
            }

            // Save new order
            for (int i = 0; i < shortcuts.Count; i++)
            {
                var keyName = $"Shortcut_{i}";
                WriteIniValue(profileIniPath, sectionName, keyName, shortcuts[i].Name);
            }
        }

        /// <summary>
        /// Updates display order when a shortcut is moved
        /// </summary>
        public static void UpdateShortcutOrder(string groupName, string shortcutName, int newPosition, List<ShortcutInfo> allShortcuts)
        {
            // Update display orders
            for (int i = 0; i < allShortcuts.Count; i++)
            {
                allShortcuts[i].DisplayOrder = i;
            }

            // Save to INI
            SaveShortcutDisplayOrder(allShortcuts, groupName);
        }

        #endregion

        #region Group Settings (INI File Management)

        /// <summary>
        /// Loads group settings from ProfileName.ini in application folder
        /// </summary>
        private static void LoadGroupSettings(GroupInfo groupInfo)
        {
            var currentProfile = GetCurrentProfile();
            var profileIniPath = Path.Combine(Application.StartupPath, currentProfile + ".ini");
            
            if (File.Exists(profileIniPath))
            {
                var sectionName = $"Group_{groupInfo.Name}";
                groupInfo.WindowStatus = ReadIniInt(profileIniPath, sectionName, "Status", 1);
                groupInfo.X = ReadIniInt(profileIniPath, sectionName, "X", 100);
                groupInfo.Y = ReadIniInt(profileIniPath, sectionName, "Y", 100);
                groupInfo.Width = ReadIniInt(profileIniPath, sectionName, "Width", 400);
                groupInfo.Height = ReadIniInt(profileIniPath, sectionName, "Height", 300);
                
                var savedName = ReadIniString(profileIniPath, sectionName, "Name", groupInfo.Name);
                if (!string.IsNullOrEmpty(savedName))
                    groupInfo.Name = savedName;
            }
        }

        /// <summary>
        /// Saves group settings to ProfileName.ini in application folder
        /// </summary>
        public static void SaveGroupSettings(GroupInfo groupInfo)
        {
            var currentProfile = GetCurrentProfile();
            var profileIniPath = Path.Combine(Application.StartupPath, currentProfile + ".ini");
            
            // For Programs group, always use "Group_Programs" section
            // For other groups, use the folder name
            string sectionName;
            if (groupInfo.Name == "Programs")
            {
                sectionName = "Group_Programs";
            }
            else
            {
                var folderName = Path.GetFileName(groupInfo.FolderPath);
                sectionName = $"Group_{folderName}";
            }
            
            WriteIniValue(profileIniPath, sectionName, "Status", groupInfo.WindowStatus.ToString());
            WriteIniValue(profileIniPath, sectionName, "X", groupInfo.X.ToString());
            WriteIniValue(profileIniPath, sectionName, "Y", groupInfo.Y.ToString());
            WriteIniValue(profileIniPath, sectionName, "Width", groupInfo.Width.ToString());
            WriteIniValue(profileIniPath, sectionName, "Height", groupInfo.Height.ToString());
            WriteIniValue(profileIniPath, sectionName, "Name", groupInfo.Name);
            
            // Also save the profile path reference
            WriteIniValue(profileIniPath, "Profile", "Path", GetGroupsFolder());
        }

        /// <summary>
        /// Creates a new profile INI file with default settings
        /// </summary>
        public static void CreateProfileIni(string profileName, string profilePath)
        {
            var profileIniPath = Path.Combine(Application.StartupPath, profileName + ".ini");
            
            // Create basic profile structure
            WriteIniValue(profileIniPath, "Profile", "Name", profileName);
            WriteIniValue(profileIniPath, "Profile", "Path", profilePath);
            WriteIniValue(profileIniPath, "Profile", "Created", DateTime.Now.ToString());
            
            // Add default Programs group if it doesn't exist
            var programsGroupSection = "Group_Programs";
            if (string.IsNullOrEmpty(ReadIniString(profileIniPath, programsGroupSection, "Name", "")))
            {
                WriteIniValue(profileIniPath, programsGroupSection, "Name", "Programs");
                WriteIniValue(profileIniPath, programsGroupSection, "Status", "0"); // 0 = Minimized by default
                WriteIniValue(profileIniPath, programsGroupSection, "X", "100");
                WriteIniValue(profileIniPath, programsGroupSection, "Y", "100");
                WriteIniValue(profileIniPath, programsGroupSection, "Width", "400");
                WriteIniValue(profileIniPath, programsGroupSection, "Height", "300");
            }
        }

        /// <summary>
        /// Deletes a profile INI file
        /// </summary>
        public static void DeleteProfileIni(string profileName)
        {
            if (profileName == "Default") return; // Cannot delete Default profile
            
            var profileIniPath = Path.Combine(Application.StartupPath, profileName + ".ini");
            if (File.Exists(profileIniPath))
            {
                File.Delete(profileIniPath);
            }
        }

        /// <summary>
        /// Gets all groups from the current profile's INI file
        /// </summary>
        public static List<GroupInfo> GetGroupsFromProfileIni()
        {
            var groups = new List<GroupInfo>();
            var currentProfile = GetCurrentProfile();
            var profileIniPath = Path.Combine(Application.StartupPath, currentProfile + ".ini");
            
            if (!File.Exists(profileIniPath))
            {
                // Create default profile INI if it doesn't exist
                CreateProfileIni(currentProfile, GetGroupsFolder());
            }
            
            // Read all sections that start with "Group_"
            var allSections = GetAllIniSections(profileIniPath);
            int groupId = 0;
            
            foreach (var sectionName in allSections.Where(s => s.StartsWith("Group_")))
            {
                var groupFolderName = sectionName.Substring(6); // Remove "Group_" prefix
                var savedName = ReadIniString(profileIniPath, sectionName, "Name", groupFolderName);
                
                // Determine folder path - Programs group uses root folder
                string folderPath;
                if (savedName == "Programs" || groupFolderName == "Programs")
                {
                    folderPath = GetGroupsFolder();
                }
                else
                {
                    folderPath = Path.Combine(GetGroupsFolder(), groupFolderName);
                }
                
                var groupInfo = new GroupInfo
                {
                    Id = groupId.ToString(),
                    Name = savedName,
                    FolderPath = folderPath,
                    WindowStatus = ReadIniInt(profileIniPath, sectionName, "Status", 1),
                    X = ReadIniInt(profileIniPath, sectionName, "X", 100),
                    Y = ReadIniInt(profileIniPath, sectionName, "Y", 100),
                    Width = ReadIniInt(profileIniPath, sectionName, "Width", 400),
                    Height = ReadIniInt(profileIniPath, sectionName, "Height", 300),
                    IconIndex = groupId
                };
                
                groups.Add(groupInfo);
                groupId++;
            }
            
            return groups;
        }

        /// <summary>
        /// Saves application settings to progman64.ini in the application folder
        /// </summary>
        public static void SaveApplicationSettings(string key, string value)
        {
            var progmanIni = Path.Combine(Application.StartupPath, "progman64.ini");
            WriteIniValue(progmanIni, "Application", key, value);
        }

        /// <summary>
        /// Loads application settings from progman64.ini
        /// </summary>
        public static string LoadApplicationSetting(string key, string defaultValue = "")
        {
            var progmanIni = Path.Combine(Application.StartupPath, "progman64.ini");
            return ReadIniString(progmanIni, "Application", key, defaultValue);
        }

        /// <summary>
        /// Gets all section names from an INI file
        /// </summary>
        private static List<string> GetAllIniSections(string iniPath)
        {
            var sections = new List<string>();
            
            if (!File.Exists(iniPath))
                return sections;
                
            try
            {
                var lines = File.ReadAllLines(iniPath);
                
                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();
                    if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
                    {
                        var sectionName = trimmedLine.Substring(1, trimmedLine.Length - 2);
                        if (!sections.Contains(sectionName))
                        {
                            sections.Add(sectionName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error reading INI sections: {ex.Message}");
            }
            
            return sections;
        }

        #endregion

        #region INI File Helper Methods

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

        private static void WriteIniValue(string iniPath, string section, string key, string value)
        {
            WritePrivateProfileString(section, key, value, iniPath);
        }

        private static string ReadIniString(string iniPath, string section, string key, string defaultValue)
        {
            var temp = new StringBuilder(255);
            GetPrivateProfileString(section, key, defaultValue, temp, 255, iniPath);
            return temp.ToString();
        }

        private static int ReadIniInt(string iniPath, string section, string key, int defaultValue)
        {
            var value = ReadIniString(iniPath, section, key, defaultValue.ToString());
            return int.TryParse(value, out int result) ? result : defaultValue;
        }

        private static Dictionary<string, string> ReadIniSection(string iniPath, string section)
        {
            var result = new Dictionary<string, string>();
            
            if (!File.Exists(iniPath))
                return result;
                
            try
            {
                var lines = File.ReadAllLines(iniPath);
                bool inSection = false;
                
                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();
                    
                    if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
                    {
                        inSection = trimmedLine.Equals($"[{section}]", StringComparison.OrdinalIgnoreCase);
                    }
                    else if (inSection && trimmedLine.Contains("="))
                    {
                        var parts = trimmedLine.Split(new[] { '=' }, 2);
                        if (parts.Length == 2)
                        {
                            result[parts[0].Trim()] = parts[1].Trim();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error reading INI section {section}: {ex.Message}");
            }
            
            return result;
        }

        #endregion

        #region Shortcut File Operations

        /// <summary>
        /// Reads a .lnk file and extracts shortcut information
        /// </summary>
        private static ShortcutInfo ReadShortcutFile(string shortcutPath)
        {
            try
            {
                // Use reflection to avoid dynamic binding issues
                Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType == null)
                    throw new InvalidOperationException("WScript.Shell not available");

                object shell = Activator.CreateInstance(shellType);
                
                object lnk = shellType.InvokeMember("CreateShortcut", 
                    System.Reflection.BindingFlags.InvokeMethod, null, shell, new object[] { shortcutPath });
                
                Type linkType = lnk.GetType();

                string targetPath = "";
                string arguments = "";
                string workingDir = "";
                string description = "";
                string iconLocation = "";

                // Safely extract properties with fallbacks
                try { targetPath = (string)linkType.InvokeMember("TargetPath", System.Reflection.BindingFlags.GetProperty, null, lnk, null) ?? ""; } catch { }
                try { arguments = (string)linkType.InvokeMember("Arguments", System.Reflection.BindingFlags.GetProperty, null, lnk, null) ?? ""; } catch { }
                try { workingDir = (string)linkType.InvokeMember("WorkingDirectory", System.Reflection.BindingFlags.GetProperty, null, lnk, null) ?? ""; } catch { }
                try { description = (string)linkType.InvokeMember("Description", System.Reflection.BindingFlags.GetProperty, null, lnk, null) ?? ""; } catch { }
                try { iconLocation = (string)linkType.InvokeMember("IconLocation", System.Reflection.BindingFlags.GetProperty, null, lnk, null) ?? ""; } catch { }

                // Expand environment variables in paths
                targetPath = ExpandEnvironmentVariables(targetPath);
                workingDir = ExpandEnvironmentVariables(workingDir);
                iconLocation = ExpandEnvironmentVariables(iconLocation);

                var shortcutInfo = new ShortcutInfo
                {
                    Name = Path.GetFileNameWithoutExtension(shortcutPath),
                    ShortcutPath = shortcutPath,
                    TargetPath = targetPath,
                    Arguments = arguments,
                    WorkingDirectory = workingDir,
                    Description = description,
                };

                // Handle icon location and index
                if (string.IsNullOrEmpty(iconLocation))
                {
                    // No specific icon set, use target path
                    shortcutInfo.IconLocation = !string.IsNullOrEmpty(targetPath) ? targetPath : shortcutPath;
                    shortcutInfo.IconIndex = 0;
                }
                else
                {
                    // Parse icon index if specified (format: "path,index")
                    if (iconLocation.Contains(","))
                    {
                        var parts = iconLocation.Split(',');
                        shortcutInfo.IconLocation = ExpandEnvironmentVariables(parts[0].Trim());
                        if (parts.Length > 1 && int.TryParse(parts[1].Trim(), out int iconIdx))
                        {
                            shortcutInfo.IconIndex = iconIdx;
                        }
                        else
                        {
                            shortcutInfo.IconIndex = 0;
                        }
                    }
                    else
                    {
                        shortcutInfo.IconLocation = iconLocation;
                        shortcutInfo.IconIndex = 0;
                    }
                }

                // Validate that we have a target path
                if (string.IsNullOrEmpty(shortcutInfo.TargetPath))
                {
                    // If no target path, this might be a special shortcut, use shortcut path as fallback
                    shortcutInfo.TargetPath = shortcutPath;
                }

                return shortcutInfo;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error reading shortcut {shortcutPath}: {ex.Message}");
                
                // Fallback: create basic info from file name
                return new ShortcutInfo
                {
                    Name = Path.GetFileNameWithoutExtension(shortcutPath),
                    ShortcutPath = shortcutPath,
                    TargetPath = shortcutPath, // Use shortcut path as fallback
                    Arguments = "",
                    WorkingDirectory = Path.GetDirectoryName(shortcutPath),
                    Description = "Shortcut (properties unavailable)",
                    IconLocation = shortcutPath,
                    IconIndex = 0
                };
            }
        }

        /// <summary>
        /// Creates a .lnk shortcut file
        /// </summary>
        private static void CreateShortcutFile(string shortcutPath, string targetPath, string arguments, string iconPath, int iconIndex)
        {
            try
            {
                // Use reflection to avoid dynamic binding issues
                Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                object shell = Activator.CreateInstance(shellType);
                
                object lnk = shellType.InvokeMember("CreateShortcut", 
                    System.Reflection.BindingFlags.InvokeMethod, null, shell, new object[] { shortcutPath });
                
                Type linkType = lnk.GetType();
                
                linkType.InvokeMember("TargetPath", System.Reflection.BindingFlags.SetProperty, null, lnk, new object[] { targetPath });
                linkType.InvokeMember("WorkingDirectory", System.Reflection.BindingFlags.SetProperty, null, lnk, new object[] { Path.GetDirectoryName(targetPath) });
                linkType.InvokeMember("Description", System.Reflection.BindingFlags.SetProperty, null, lnk, new object[] { "Created by Program Manager" });

                if (!string.IsNullOrEmpty(arguments))
                    linkType.InvokeMember("Arguments", System.Reflection.BindingFlags.SetProperty, null, lnk, new object[] { arguments });

                if (!string.IsNullOrEmpty(iconPath))
                    linkType.InvokeMember("IconLocation", System.Reflection.BindingFlags.SetProperty, null, lnk, new object[] { iconPath + "," + iconIndex });
                else
                    linkType.InvokeMember("IconLocation", System.Reflection.BindingFlags.SetProperty, null, lnk, new object[] { targetPath + ",0" });

                linkType.InvokeMember("Save", System.Reflection.BindingFlags.InvokeMethod, null, lnk, null);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to create shortcut: {ex.Message}", ex);
            }
        }

        #endregion

        #region Profile Management

        /// <summary>
        /// Saves a profile to progman64.ini and creates the profile INI file
        /// </summary>
        public static void SaveProfile(string name, string path)
        {
            var progmanIni = Path.Combine(Application.StartupPath, "progman64.ini");
            WriteIniValue(progmanIni, "Profiles", name, path);
            
            // Create the profile INI file
            CreateProfileIni(name, path);
        }

        /// <summary>
        /// Deletes a profile from progman64.ini and removes the profile INI file
        /// </summary>
        public static void DeleteProfile(string name)
        {
            if (name == "Default") return; // Cannot delete Default profile
            
            var progmanIni = Path.Combine(Application.StartupPath, "progman64.ini");
            WriteIniValue(progmanIni, "Profiles", name, ""); // Empty value effectively deletes
            
            // Delete the profile INI file
            DeleteProfileIni(name);
        }

        /// <summary>
        /// Sets the current active profile and ensures the profile INI exists
        /// </summary>
        public static void SetCurrentProfile(string profileName)
        {
            SaveApplicationSettings("current_profile", profileName);
            
            // Set the groups folder based on the profile
            var profiles = GetAllProfiles();
            var profile = profiles.FirstOrDefault(p => p.Name == profileName);
            if (profile != null)
            {
                SetGroupsFolder(profile.Path);
                
                // Ensure the profile INI file exists
                var profileIniPath = Path.Combine(Application.StartupPath, profileName + ".ini");
                if (!File.Exists(profileIniPath))
                {
                    CreateProfileIni(profileName, profile.Path);
                }
            }
        }

        #endregion

        #region Profile Helper Methods

        /// <summary>
        /// Gets all profiles configured in progman64.ini
        /// </summary>
        public static List<ProfileInfo> GetAllProfiles()
        {
            var profiles = new List<ProfileInfo>();
            var progmanIni = Path.Combine(Application.StartupPath, "progman64.ini");
            
            // Always add the Default profile first
            profiles.Add(new ProfileInfo
            {
                Name = "Default",
                Path = Path.Combine(Application.StartupPath, "Shortcuts"),
                IsDefault = true
            });
            
            // Add default Start Menu profiles
            var allUsersStartMenu = @"C:\ProgramData\Microsoft\Windows\Start Menu\Programs";
            var currentUserStartMenu = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
                @"Microsoft\Windows\Start Menu\Programs");
            
            profiles.Add(new ProfileInfo
            {
                Name = "Start Menu (All Users)",
                Path = allUsersStartMenu,
                IsDefault = false
            });
            
            profiles.Add(new ProfileInfo
            {
                Name = "Start Menu (Current User)",
                Path = currentUserStartMenu,
                IsDefault = false
            });
            
            if (File.Exists(progmanIni))
            {
                // Read custom profiles from [Profiles] section
                var profilesSection = ReadIniSection(progmanIni, "Profiles");
                foreach (var profile in profilesSection)
                {
                    // Skip if it's one of our default profiles
                    if (profile.Key != "Default" && 
                        profile.Key != "Start Menu (All Users)" && 
                        profile.Key != "Start Menu (Current User)" && 
                        !string.IsNullOrEmpty(profile.Value))
                    {
                        profiles.Add(new ProfileInfo
                        {
                            Name = profile.Key,
                            Path = profile.Value,
                            IsDefault = false
                        });
                    }
                }
            }
            
            return profiles;
        }

        /// <summary>
        /// Gets the current active profile name
        /// </summary>
        public static string GetCurrentProfile()
        {
            return LoadApplicationSetting("current_profile", "Default");
        }

        #endregion

        #region Environment Variable Expansion

        /// <summary>
        /// Expands environment variables in a path string (e.g., %SystemRoot%, %HOMEDRIVE%%HOMEPATH%)
        /// </summary>
        /// <param name="path">Path that may contain environment variables</param>
        /// <returns>Expanded path with environment variables resolved</returns>
        public static string ExpandEnvironmentVariables(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;

            try
            {
                // Use Environment.ExpandEnvironmentVariables to resolve DOS-style variables
                return Environment.ExpandEnvironmentVariables(path);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error expanding environment variables in path '{path}': {ex.Message}");
                return path; // Return original path if expansion fails
            }
        }

        /// <summary>
        /// Safely checks if a path (with potential environment variables) exists after expansion
        /// </summary>
        /// <param name="path">Path that may contain environment variables</param>
        /// <returns>True if the expanded path exists</returns>
        public static bool FileExistsWithExpansion(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            try
            {
                string expandedPath = ExpandEnvironmentVariables(path);
                return File.Exists(expandedPath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking file existence for path '{path}': {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Safely checks if a directory (with potential environment variables) exists after expansion
        /// </summary>
        /// <param name="path">Path that may contain environment variables</param>
        /// <returns>True if the expanded path exists as a directory</returns>
        public static bool DirectoryExistsWithExpansion(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            try
            {
                string expandedPath = ExpandEnvironmentVariables(path);
                return Directory.Exists(expandedPath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking directory existence for path '{path}': {ex.Message}");
                return false;
            }
        }

        #endregion
    }

    #region Data Classes

    /// <summary>
    /// Represents a program group
    /// </summary>
    public class GroupInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string FolderPath { get; set; }
        public int WindowStatus { get; set; } // 0=Minimized, 1=Normal, 2=Maximized
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int IconIndex { get; set; }
    }

    /// <summary>
    /// Represents a shortcut file
    /// </summary>
    public class ShortcutInfo
    {
        public string Name { get; set; }
        public string ShortcutPath { get; set; }
        public string TargetPath { get; set; }
        public string Arguments { get; set; }
        public string WorkingDirectory { get; set; }
        public string Description { get; set; }
        public string IconLocation { get; set; }
        public int IconIndex { get; set; }
        public int DisplayOrder { get; set; }
    }

    /// <summary>
    /// Represents a profile (reference to a folder location)
    /// </summary>
    public class ProfileInfo
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public bool IsDefault { get; set; }
    }

    #endregion
}
