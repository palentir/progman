using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.IO;

namespace ProgramManagerVC
{
    public partial class FormChild : Form
    {
        private FormWindowState previousWindowState;
        private Icon originalIcon;
        private int currentIconSize = 32; // Default icon size

        public FormChild()
        {
            InitializeComponent();
            previousWindowState = FormWindowState.Normal;
            originalIcon = this.Icon;
        }

        private void FormChild_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.MdiFormClosing)
            {
                // Main form is closing - save window state before closing
                SaveWindowState();
                e.Cancel = false;
            }
            else
            {
                // User clicked X button - minimize instead of close
                this.WindowState = FormWindowState.Minimized;
                e.Cancel = true;
            }
        }

        private void FormChild_Load(object sender, EventArgs e)
        {
            // Load saved icon size from INI
            LoadIconSizeFromINI();

            InitializeItems();
            if (System.Environment.OSVersion.Version.Major < 6) {
                runAsAdministratorToolStripMenuItem.Visible = false;
            } else {
                runAsAdministratorToolStripMenuItem.Image = SystemIcons.Shield.ToBitmap();
            }

            // Configure ListView for better icon display
            listViewMain.View = View.LargeIcon;

            // The larger ImageList size (48x48) will automatically provide better spacing

            previousWindowState = this.WindowState;
            this.Resize += FormChild_Resize;

            // Hook up the ListMenu opening event to control delete button
            this.ListMenu.Opening += ListMenu_Opening;

            // Enable drag & drop for the ListView
            listViewMain.AllowDrop = true;
            listViewMain.ItemDrag += ListViewMain_ItemDrag;
            listViewMain.DragEnter += ListViewMain_DragEnter;
            listViewMain.DragDrop += ListViewMain_DragDrop;
            listViewMain.DragOver += ListViewMain_DragOver;

            // Also enable drag & drop for the form itself (to catch drops anywhere)
            this.AllowDrop = true;
            this.DragEnter += FormChild_DragEnter;
            this.DragDrop += FormChild_DragDrop;

            // Enable mouse wheel for icon sizing
            this.MouseWheel += FormChild_MouseWheel;
            listViewMain.MouseWheel += FormChild_MouseWheel;
        }
        
        private void ListMenu_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Disable delete for protected groups
            string groupName = this.Text;
            bool isProtected = IsProtectedGroup(groupName);
            
            deleteToolStripMenuItem1.Enabled = !isProtected;
        }
        
        private bool IsProtectedGroup(string groupName)
        {
            // Protect Default profile built-in groups and Start Menu groups
            var currentProfile = JsonBasedData.GetCurrentProfile();
            
            if (currentProfile == "Default")
            {
                // In Default profile, protect "Default", "Programs", and "Startup"
                return groupName == "Default" || 
                       groupName == "Programs" || 
                       groupName == "Startup";
            }
            
            return false;
        }

        private void FormChild_Resize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized && previousWindowState != FormWindowState.Minimized)
            {
                this.Visible = false;
                FormMain mainForm = this.MdiParent as FormMain;
                if (mainForm != null)
                {
                    mainForm.AddMinimizedIcon(this);
                }
            }
            else if (this.WindowState != FormWindowState.Minimized && previousWindowState == FormWindowState.Minimized)
            {
                this.Visible = true;
                this.Icon = originalIcon;
                FormMain mainForm = this.MdiParent as FormMain;
                if (mainForm != null)
                {
                    mainForm.RemoveMinimizedIcon(this);
                }

                // Refresh shortcuts when window is restored from minimized
                this.BeginInvoke(new Action(() => InitializeItems()));
            }

            previousWindowState = this.WindowState;
        }

        private void ListViewMain_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (listViewMain.SelectedItems.Count > 0)
            {
                var shortcutInfo = (ShortcutInfo)listViewMain.SelectedItems[0].Tag;
                
                // Expand environment variables before checking file existence
                string expandedTargetPath = JsonBasedData.ExpandEnvironmentVariables(shortcutInfo.TargetPath);
                
                // Check if file exists before trying to launch it
                if (System.IO.File.Exists(expandedTargetPath))
                {
                    try
                    {
                        var psi = new ProcessStartInfo();
                        psi.FileName = expandedTargetPath;
                        psi.Arguments = JsonBasedData.ExpandEnvironmentVariables(shortcutInfo.Arguments ?? "");
                        
                        string expandedWorkingDir = JsonBasedData.ExpandEnvironmentVariables(shortcutInfo.WorkingDirectory ?? "");
                        if (string.IsNullOrEmpty(expandedWorkingDir))
                            expandedWorkingDir = Path.GetDirectoryName(expandedTargetPath);
                        
                        psi.WorkingDirectory = expandedWorkingDir;
                        psi.UseShellExecute = true;
                        Process.Start(psi);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Could not open file:\n\n" + expandedTargetPath + 
                                       "\n\nError: " + ex.Message, 
                                       "Error Opening File", 
                                       MessageBoxButtons.OK, 
                                       MessageBoxIcon.Error);
                    }
                }
                else
                {
                    MessageBox.Show("The file does not exist:\n\n" + expandedTargetPath + 
                                   "\n\nThe file may have been moved, renamed, or deleted. " +
                                   "You can edit this item's properties to update the path.", 
                                   "File Not Found", 
                                   MessageBoxButtons.OK, 
                                   MessageBoxIcon.Warning);
                }
            }
        }

        public void InitializeItems()
        {
            listViewMain.Items.Clear();
            imageListIcons.Images.Clear();
            
            var groupName = GetGroupNameFromId(this.Tag?.ToString() ?? "");
            if (string.IsNullOrEmpty(groupName)) return;

            // Use the root-handling version to catch .lnk files in root folder
            var shortcuts = JsonBasedData.GetShortcutsInGroupWithRoot(groupName);
            
            for (int i = 0; i < shortcuts.Count; i++)
            {
                var shortcut = shortcuts[i];
                
                try
                {
                    // Try to load the icon with multiple fallback strategies
                    Icon extractedIcon = ExtractIconFromShortcut(shortcut);
                    
                    // Add the icon to the image list
                    if (extractedIcon != null)
                    {
                        // Resize icon to fit the current icon size
                        var bitmap = new Bitmap(extractedIcon.ToBitmap(), new Size(currentIconSize, currentIconSize));
                        imageListIcons.Images.Add(bitmap);
                    }
                    else
                    {
                        // Ultimate fallback - use a default application icon
                        var bitmap = new Bitmap(SystemIcons.Application.ToBitmap(), new Size(currentIconSize, currentIconSize));
                        imageListIcons.Images.Add(bitmap);
                    }
                    
                    ListViewItem item = new ListViewItem();
                    item.Text = shortcut.Name;
                    item.ImageIndex = i;
                    
                    // Build tooltip with target and arguments, showing environment variables if present
                    string tooltip = shortcut.TargetPath;

                    // Check if target contains environment variables and show both versions
                    if (JsonBasedData.ContainsEnvironmentVariables(shortcut.TargetPath))
                    {
                        string expandedPath = JsonBasedData.ExpandEnvironmentVariablesEnhanced(shortcut.TargetPath);
                        tooltip = $"{shortcut.TargetPath}\n→ {expandedPath}";
                    }
                    else
                    {
                        // Try the old method as fallback
                        string expandedPath = JsonBasedData.ExpandEnvironmentVariables(shortcut.TargetPath);
                        if (shortcut.TargetPath != expandedPath)
                        {
                            tooltip = $"{shortcut.TargetPath}\n→ {expandedPath}";
                        }
                    }

                    if (!string.IsNullOrEmpty(shortcut.Arguments))
                    {
                        if (JsonBasedData.ContainsEnvironmentVariables(shortcut.Arguments))
                        {
                            string expandedArgs = JsonBasedData.ExpandEnvironmentVariablesEnhanced(shortcut.Arguments);
                            tooltip += $"\nArguments: {shortcut.Arguments}\n→ {expandedArgs}";
                        }
                        else
                        {
                            string expandedArgs = JsonBasedData.ExpandEnvironmentVariables(shortcut.Arguments);
                            if (shortcut.Arguments != expandedArgs)
                            {
                                tooltip += $"\nArguments: {shortcut.Arguments}\n→ {expandedArgs}";
                            }
                            else
                            {
                                tooltip += $"\nArguments: {shortcut.Arguments}";
                            }
                        }
                    }
                    
                    item.ToolTipText = tooltip;
                    
                    item.Tag = shortcut; // Store the entire shortcut info
                    listViewMain.Items.Add(item);
                }
                catch (Exception ex)
                {
                    // Even if there's an exception, still show the item
                    System.Diagnostics.Debug.WriteLine($"Error processing shortcut {shortcut.Name}: {ex.Message}");

                    var bitmap = new Bitmap(SystemIcons.Error.ToBitmap(), new Size(currentIconSize, currentIconSize));
                    imageListIcons.Images.Add(bitmap);
                    
                    ListViewItem item = new ListViewItem();
                    item.Text = shortcut.Name;
                    item.ImageIndex = i;
                    item.ToolTipText = shortcut.TargetPath + " (Error loading)";
                    item.Tag = shortcut;
                    listViewMain.Items.Add(item);
                }
            }
        }

        /// <summary>
        /// Extracts icon from shortcut with multiple fallback strategies
        /// </summary>
        private Icon ExtractIconFromShortcut(ShortcutInfo shortcut)
        {
            Icon extractedIcon = null;

            // Strategy 1: Try to load icon from the specified icon location
            if (!string.IsNullOrEmpty(shortcut.IconLocation))
            {
                string expandedIconPath = JsonBasedData.ExpandEnvironmentVariables(shortcut.IconLocation);
                if (File.Exists(expandedIconPath))
                {
                    extractedIcon = TryExtractIconFromFile(expandedIconPath, shortcut.IconIndex);
                    if (extractedIcon != null) return extractedIcon;
                }
            }

            // Strategy 2: Try to load icon from the target executable
            if (!string.IsNullOrEmpty(shortcut.TargetPath))
            {
                string expandedTargetPath = JsonBasedData.ExpandEnvironmentVariables(shortcut.TargetPath);
                if (File.Exists(expandedTargetPath))
                {
                    extractedIcon = TryExtractIconFromFile(expandedTargetPath, 0);
                    if (extractedIcon != null) return extractedIcon;
                }
            }

            // Strategy 3: Try to get associated icon for the target file
            if (!string.IsNullOrEmpty(shortcut.TargetPath))
            {
                string expandedTargetPath = JsonBasedData.ExpandEnvironmentVariables(shortcut.TargetPath);
                if (File.Exists(expandedTargetPath))
                {
                    try
                    {
                        extractedIcon = Icon.ExtractAssociatedIcon(expandedTargetPath);
                        if (extractedIcon != null) return extractedIcon;
                    }
                    catch { }
                }
            }

            // Strategy 4: Try to get associated icon for the shortcut file itself
            try
            {
                extractedIcon = Icon.ExtractAssociatedIcon(shortcut.ShortcutPath);
                if (extractedIcon != null) return extractedIcon;
            }
            catch { }

            // Strategy 5: Use system warning icon for missing files
            if (!string.IsNullOrEmpty(shortcut.TargetPath))
            {
                string expandedTargetPath = JsonBasedData.ExpandEnvironmentVariables(shortcut.TargetPath);
                if (!File.Exists(expandedTargetPath))
                {
                    return SystemIcons.Warning;
                }
            }

            return null; // Will fall back to SystemIcons.Application in calling method
        }

        /// <summary>
        /// Tries to extract an icon from a file (exe, dll, or ico)
        /// </summary>
        private Icon TryExtractIconFromFile(string filePath, int iconIndex)
        {
            try
            {
                // Expand environment variables in the file path
                string expandedPath = JsonBasedData.ExpandEnvironmentVariables(filePath);
                
                if (!File.Exists(expandedPath)) return null;

                string extension = Path.GetExtension(expandedPath).ToLower();
                
                if (extension == ".ico")
                {
                    return new Icon(expandedPath);
                }
                else if (extension == ".exe" || extension == ".dll")
                {
                    // Extract specific icon by index using Windows Shell API
                    IntPtr hIcon = ExtractIcon(IntPtr.Zero, expandedPath, iconIndex);
                    if (hIcon != IntPtr.Zero && hIcon != (IntPtr)1)
                    {
                        Icon icon = Icon.FromHandle(hIcon);
                        // Don't destroy the icon handle here, let GC handle it
                        return icon;
                    }
                }
                
                // Fallback to associated icon
                return Icon.ExtractAssociatedIcon(expandedPath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error extracting icon from {filePath}: {ex.Message}");
                return null;
            }
        }

        private string GetGroupNameFromId(string id)
        {
            var groups = JsonBasedData.GetAllGroups();
            var group = groups.FirstOrDefault(g => g.Id == id);
            return group?.Name ?? "";
        }

        // Windows API function for extracting icons
        [System.Runtime.InteropServices.DllImport("shell32.dll")]
        private static extern IntPtr ExtractIcon(IntPtr hInst, string lpszExeFileName, int nIconIndex);

        private void FormChild_ResizeEnd(object sender, EventArgs e)
        {
            SaveWindowState();
        }

        private void FormChild_Move(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Normal)
            {
                SaveWindowState();
            }
        }

        private void SaveWindowState()
        {
            if (this.Tag != null)
            {
                var groups = JsonBasedData.GetAllGroups();
                var group = groups.FirstOrDefault(g => g.Id == this.Tag.ToString());
                
                if (group != null)
                {
                    // Update group info with current window state
                    group.Name = this.Text;
                    
                    if (this.WindowState == FormWindowState.Minimized)
                        group.WindowStatus = 0;
                    else if (this.WindowState == FormWindowState.Maximized)
                        group.WindowStatus = 2;
                    else
                        group.WindowStatus = 1;

                    if (this.WindowState == FormWindowState.Normal)
                    {
                        group.X = this.Location.X;
                        group.Y = this.Location.Y;
                        group.Width = this.Width;
                        group.Height = this.Height;
                    }
                    
                    // Save to .ini file in application folder
                    JsonBasedData.SaveGroupSettings(group);
                }
            }
        }

        private void listViewMain_MouseDown(object sender, MouseEventArgs e) 
        {
            if (e.Button == MouseButtons.Right) 
            {
                ListViewItem itemAtPoint = listViewMain.GetItemAt(e.X, e.Y);
                
                if (itemAtPoint != null)
                {
                    // Right-clicked on an item - show item context menu
                    if (!itemAtPoint.Selected)
                    {
                        listViewMain.SelectedItems.Clear();
                        itemAtPoint.Selected = true;
                        itemAtPoint.Focused = true;
                    }
                    FileMenu.Show(Cursor.Position);
                }
                else
                {
                    // Right-clicked on empty space - show group context menu
                    listViewMain.SelectedItems.Clear();
                    ListMenu.Show(Cursor.Position);
                }
            }
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e) 
        {
            if (listViewMain.SelectedItems.Count > 0)
            {
                var shortcutInfo = (ShortcutInfo)listViewMain.SelectedItems[0].Tag;
                
                // Expand environment variables before checking file existence
                string expandedTargetPath = JsonBasedData.ExpandEnvironmentVariables(shortcutInfo.TargetPath);
                
                // Check if file exists before trying to launch it
                if (System.IO.File.Exists(expandedTargetPath))
                {
                    try
                    {
                        var psi = new ProcessStartInfo();
                        psi.FileName = expandedTargetPath;
                        psi.Arguments = JsonBasedData.ExpandEnvironmentVariables(shortcutInfo.Arguments ?? "");
                        
                        string expandedWorkingDir = JsonBasedData.ExpandEnvironmentVariables(shortcutInfo.WorkingDirectory ?? "");
                        if (string.IsNullOrEmpty(expandedWorkingDir))
                            expandedWorkingDir = Path.GetDirectoryName(expandedTargetPath);
                        
                        psi.WorkingDirectory = expandedWorkingDir;
                        psi.UseShellExecute = true;
                        Process.Start(psi);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Could not open file:\n\n" + expandedTargetPath + 
                                       "\n\nError: " + ex.Message, 
                                       "Error Opening File", 
                                       MessageBoxButtons.OK, 
                                       MessageBoxIcon.Error);
                    }
                }
                else
                {
                    MessageBox.Show("The file does not exist:\n\n" + expandedTargetPath + 
                                   "\n\nThe file may have been moved, renamed, or deleted. " +
                                   "You can edit this item's properties to update the path.", 
                                   "File Not Found", 
                                   MessageBoxButtons.OK, 
                                   MessageBoxIcon.Warning);
                }
            }
        }

        private void editToolStripMenuItem_Click(object sender, EventArgs e) 
        {
            if (listViewMain.SelectedItems.Count > 0)
            {
                var shortcutInfo = (ShortcutInfo)listViewMain.SelectedItems[0].Tag;
                
                // Expand environment variables before checking file existence
                string expandedTargetPath = JsonBasedData.ExpandEnvironmentVariables(shortcutInfo.TargetPath);
                
                // Check if file exists before trying to show it in explorer
                if (System.IO.File.Exists(expandedTargetPath))
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo("explorer.exe", "/select, \"" + expandedTargetPath + "\""));
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Could not open file location:\n\n" + expandedTargetPath + 
                                       "\n\nError: " + ex.Message, 
                                       "Error", 
                                       MessageBoxButtons.OK, 
                                       MessageBoxIcon.Error);
                    }
                }
                else
                {
                    MessageBox.Show("The file does not exist:\n\n" + expandedTargetPath + 
                                   "\n\nThe file may have been moved, renamed, or deleted. " +
                                   "You can edit this item's properties to update the path.", 
                                   "File Not Found", 
                                   MessageBoxButtons.OK, 
                                   MessageBoxIcon.Warning);
                }
            }
        }

        private void runAsAdministratorToolStripMenuItem_Click(object sender, EventArgs e) 
        {
            if (System.Environment.OSVersion.Version.Major >= 6) 
            {
                if (listViewMain.SelectedItems.Count > 0)
                {
                    var shortcutInfo = (ShortcutInfo)listViewMain.SelectedItems[0].Tag;
                    
                    // Expand environment variables before checking file existence
                    string expandedTargetPath = JsonBasedData.ExpandEnvironmentVariables(shortcutInfo.TargetPath);
                    
                    // Check if file exists before trying to run it
                    if (System.IO.File.Exists(expandedTargetPath))
                    {
                        try 
                        {
                            Process proc = new Process();
                            proc.StartInfo.FileName = expandedTargetPath;
                            proc.StartInfo.Arguments = JsonBasedData.ExpandEnvironmentVariables(shortcutInfo.Arguments ?? "");
                            proc.StartInfo.UseShellExecute = true;
                            proc.StartInfo.Verb = "runas";
                            proc.Start();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Could not run file as administrator:\n\n" + expandedTargetPath + 
                                           "\n\nError: " + ex.Message, 
                                           "Error", 
                                           MessageBoxButtons.OK, 
                                           MessageBoxIcon.Error);
                        }
                    }
                    else
                    {
                        MessageBox.Show("The file does not exist:\n\n" + expandedTargetPath + 
                                       "\n\nThe file may have been moved, renamed, or deleted. " +
                                       "You can edit this item's properties to update the path.", 
                                       "File Not Found", 
                                       MessageBoxButtons.OK, 
                                       MessageBoxIcon.Warning);
                    }
                }
            }
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e) 
        {
            if (listViewMain.SelectedItems.Count > 0)
            {
                var selectedItem = listViewMain.SelectedItems[0];
                var shortcutInfo = (ShortcutInfo)selectedItem.Tag;
                
                // Show confirmation with file name
                string message = $"Are you sure you want to permanently delete the file:\n\n{selectedItem.Text}.lnk";
                
                if (MessageBox.Show(message, "Confirm Delete",
                                       MessageBoxButtons.YesNo,
                                       MessageBoxIcon.Question) == DialogResult.Yes) 
                {
                    try
                    {
                        // Delete the .lnk file
                        if (File.Exists(shortcutInfo.ShortcutPath))
                        {
                            File.Delete(shortcutInfo.ShortcutPath);
                        }

                        // Update icon ordering after deletion
                        var groupName = GetGroupNameFromId(this.Tag?.ToString() ?? "");
                        var shortcuts = JsonBasedData.GetShortcutsInGroupWithRoot(groupName);
                        for (int i = 0; i < shortcuts.Count; i++)
                        {
                            shortcuts[i].DisplayOrder = i;
                        }
                        JsonBasedData.SaveShortcutDisplayOrder(shortcuts, groupName);

                        InitializeItems();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error deleting file: {ex.Message}",
                            "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void newItemToolStripMenuItem_Click(object sender, EventArgs e) 
        {
            using (FormCreateItem createform = new FormCreateItem(this.Tag?.ToString() ?? "")) 
            {
                if (createform.ShowDialog() == DialogResult.OK) 
                {
                    InitializeItems();
                }
            }
        }

        private void propertiesToolStripMenuItem_Click(object sender, EventArgs e) 
        {
            if (listViewMain.SelectedItems.Count > 0)
            {
                var selectedItem = listViewMain.SelectedItems[0];
                using (FormCreateItem createform = new FormCreateItem(this.Tag?.ToString() ?? "", selectedItem.Text))
                {
                    if (createform.ShowDialog() == DialogResult.OK)
                    {
                        InitializeItems();
                    }
                }
            }
        }

        private void propertiesToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            Form createForm = new FormCreateGroup(this.Tag?.ToString() ?? "");
            if (createForm.ShowDialog() == DialogResult.OK)
            {
                var groups = JsonBasedData.GetAllGroups();
                var group = groups.FirstOrDefault(g => g.Id == this.Tag?.ToString());
                if (group != null)
                {
                    this.Text = group.Name;
                }
                InitializeItems();
            }
        }

        private void deleteToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            var groupName = GetGroupNameFromId(this.Tag?.ToString() ?? "");
            
            // Special handling for Programs group
            if (groupName == "Programs")
            {
                var profilePath = JsonBasedData.GetGroupsFolder();
                string message = $"Are you sure you want to delete all shortcuts in:\n\n{profilePath}";
                
                if (MessageBox.Show(message, "Confirm Delete All Shortcuts",
                                   MessageBoxButtons.YesNo,
                                   MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    try
                    {
                        // Delete all .lnk files in the root of the profile folder
                        var rootLnkFiles = Directory.GetFiles(profilePath, "*.lnk", SearchOption.TopDirectoryOnly);
                        foreach (var lnkFile in rootLnkFiles)
                        {
                            File.Delete(lnkFile);
                        }
                        
                        InitializeItems();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error deleting shortcuts: {ex.Message}",
                            "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            else
            {
                // Regular group deletion
                string message = $"Are you sure you want to permanently delete the folder:\n\n{groupName}";
                
                if (MessageBox.Show(message, "Confirm Delete Folder",
                                   MessageBoxButtons.YesNo,
                                   MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    try
                    {
                        JsonBasedData.DeleteGroup(groupName);
                        this.Hide();
                        this.DestroyHandle();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error deleting group: {ex.Message}",
                            "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void ListViewMain_ItemDrag(object sender, ItemDragEventArgs e)
        {
            if (e.Item is ListViewItem item && item.Tag is ShortcutInfo shortcut)
            {
                // Create a data object containing both the ListViewItem (for same-window reordering) 
                // and file path (for cross-window copying)
                var dataObject = new DataObject();

                // Add the ListViewItem for internal reordering
                dataObject.SetData(typeof(ListViewItem), item);

                // Add file collection for copying to other windows/applications
                var fileCollection = new System.Collections.Specialized.StringCollection();
                fileCollection.Add(shortcut.ShortcutPath);
                dataObject.SetFileDropList(fileCollection);

                listViewMain.DoDragDrop(dataObject, DragDropEffects.Move | DragDropEffects.Copy);
            }
        }

        private void ListViewMain_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(ListViewItem)))
            {
                // Check if it's from the same ListView (same window reordering)
                var draggedItem = (ListViewItem)e.Data.GetData(typeof(ListViewItem));
                if (draggedItem != null && draggedItem.ListView == listViewMain)
                {
                    // Handle internal drag & drop for reordering within same window
                    Point targetPoint = listViewMain.PointToClient(new Point(e.X, e.Y));
                    ListViewItem targetItem = listViewMain.GetItemAt(targetPoint.X, targetPoint.Y);

                    // Check if we're over the same item we're dragging
                    if (targetItem != null && targetItem == draggedItem)
                    {
                        // Over the same item - show no-drop cursor
                        e.Effect = DragDropEffects.None;
                        Cursor.Current = Cursors.No;
                    }
                    else if (targetItem != null)
                    {
                        // Over a different existing item - show no-drop cursor
                        e.Effect = DragDropEffects.None;
                        Cursor.Current = Cursors.No;
                    }
                    else
                    {
                        // Over empty space - allow drop for reordering
                        e.Effect = DragDropEffects.Move;
                        Cursor.Current = Cursors.Default;
                    }
                }
                else
                {
                    // Different window - show copy cursor
                    e.Effect = DragDropEffects.Copy;
                }
            }
            else if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // Handle external file drop
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void ListViewMain_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(ListViewItem)) || e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Move | DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void ListViewMain_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(ListViewItem)))
            {
                var draggedItem = (ListViewItem)e.Data.GetData(typeof(ListViewItem));

                if (draggedItem != null && draggedItem.ListView == listViewMain)
                {
                    // Same window - handle reordering
                    Point targetPoint = listViewMain.PointToClient(new Point(e.X, e.Y));
                    ListViewItem targetItem = listViewMain.GetItemAt(targetPoint.X, targetPoint.Y);

                    if (targetItem == null)
                    {
                        // Dropped on empty space - calculate new position
                        int newIndex = CalculateDropIndex(targetPoint);
                        ReorderShortcut(draggedItem, newIndex);
                    }
                }
                else if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    // Different window or external drop - handle file copying
                    HandleFileDrop(e);
                }
            }
            else if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // Handle external file drop (from Explorer, etc.)
                HandleExternalFileDrop(e);
            }
        }

        private int CalculateDropIndex(Point targetPoint)
        {
            // For empty list, insert at beginning
            if (listViewMain.Items.Count == 0)
                return 0;

            // Get the dragged item to exclude it from calculations
            var draggedItem = listViewMain.SelectedItems.Count > 0 ? listViewMain.SelectedItems[0] : null;

            // Calculate grid layout based on first item
            var firstItem = listViewMain.Items[0];
            int itemWidth = firstItem.Bounds.Width;
            int itemHeight = firstItem.Bounds.Height;

            // Calculate items per row based on ListView width
            int itemsPerRow = Math.Max(1, listViewMain.ClientRectangle.Width / itemWidth);

            // Find insertion position by checking each potential slot
            for (int i = 0; i < listViewMain.Items.Count; i++)
            {
                var item = listViewMain.Items[i];

                // Skip the dragged item in our calculations
                if (item == draggedItem)
                    continue;

                // Calculate the "slot" boundaries for this item
                int row = i / itemsPerRow;
                int col = i % itemsPerRow;

                Rectangle itemBounds = item.Bounds;

                // Check if drop point is in the left half of this item's slot
                if (targetPoint.X < itemBounds.X + itemBounds.Width / 2 && 
                    targetPoint.Y >= itemBounds.Y && 
                    targetPoint.Y <= itemBounds.Bottom)
                {
                    // Insert before this item
                    return i;
                }

                // Check if drop point is in the right half of this item's slot
                if (targetPoint.X >= itemBounds.X + itemBounds.Width / 2 && 
                    targetPoint.X <= itemBounds.Right &&
                    targetPoint.Y >= itemBounds.Y && 
                    targetPoint.Y <= itemBounds.Bottom)
                {
                    // Insert after this item
                    return i + 1;
                }

                // Check if drop point is below this row
                if (targetPoint.Y > itemBounds.Bottom)
                {
                    // Continue to check next items
                    continue;
                }
            }

            // If we get here, drop at the end
            return listViewMain.Items.Count;
        }

        private void ReorderShortcut(ListViewItem draggedItem, int newIndex)
        {
            try
            {
                var groupName = GetGroupNameFromId(this.Tag?.ToString() ?? "");
                var shortcuts = JsonBasedData.GetShortcutsInGroupWithRoot(groupName);

                // Find the dragged shortcut
                var draggedShortcut = (ShortcutInfo)draggedItem.Tag;
                var oldIndex = shortcuts.FindIndex(s => s.Name == draggedShortcut.Name);

                if (oldIndex >= 0 && newIndex != oldIndex && newIndex != oldIndex + 1)
                {
                    // Remove from old position
                    shortcuts.RemoveAt(oldIndex);

                    // Adjust new index if we removed an item before the insertion point
                    int adjustedIndex = newIndex;
                    if (newIndex > oldIndex)
                        adjustedIndex = newIndex - 1;

                    // Ensure index is within bounds
                    adjustedIndex = Math.Max(0, Math.Min(adjustedIndex, shortcuts.Count));

                    // Insert at new position
                    if (adjustedIndex >= shortcuts.Count)
                        shortcuts.Add(draggedShortcut);
                    else
                        shortcuts.Insert(adjustedIndex, draggedShortcut);

                    // Update display orders
                    for (int i = 0; i < shortcuts.Count; i++)
                    {
                        shortcuts[i].DisplayOrder = i;
                    }

                    // Save the new order
                    JsonBasedData.SaveShortcutDisplayOrder(shortcuts, groupName);

                    // Refresh the display
                    InitializeItems();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reordering shortcut: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void HandleFileDrop(DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    string[] droppedItems = (string[])e.Data.GetData(DataFormats.FileDrop);
                    var groupName = GetGroupNameFromId(this.Tag?.ToString() ?? "");

                    if (string.IsNullOrEmpty(groupName))
                        return;

                    foreach (string itemPath in droppedItems)
                    {
                        try
                        {
                            if (File.Exists(itemPath))
                            {
                                // It's a file
                                string fileName = Path.GetFileName(itemPath);
                                string extension = Path.GetExtension(itemPath).ToLower();

                                if (extension == ".lnk")
                                {
                                    // Copy .lnk file directly
                                    var destPath = Path.Combine(JsonBasedData.GetGroupsFolder(), groupName, fileName);
                                    if (groupName == "Programs")
                                    {
                                        // For Programs group, copy to root if no subfolder exists
                                        var groupFolder = Path.Combine(JsonBasedData.GetGroupsFolder(), groupName);
                                        if (!Directory.Exists(groupFolder))
                                            destPath = Path.Combine(JsonBasedData.GetGroupsFolder(), fileName);
                                    }

                                    File.Copy(itemPath, destPath, true);
                                }
                                else
                                {
                                    // Create shortcut for the file
                                    string shortcutName = Path.GetFileNameWithoutExtension(fileName);
                                    JsonBasedData.CreateShortcut(groupName, shortcutName, itemPath, "", itemPath, 0);
                                }
                            }
                            else if (Directory.Exists(itemPath))
                            {
                                // It's a folder - copy all .lnk files from it
                                string folderName = Path.GetDirectoryName(itemPath);
                                var lnkFiles = Directory.GetFiles(itemPath, "*.lnk", SearchOption.AllDirectories);

                                foreach (var lnkFile in lnkFiles)
                                {
                                    string lnkFileName = Path.GetFileName(lnkFile);
                                    var destPath = Path.Combine(JsonBasedData.GetGroupsFolder(), groupName, lnkFileName);
                                    if (groupName == "Programs")
                                    {
                                        var groupFolder = Path.Combine(JsonBasedData.GetGroupsFolder(), groupName);
                                        if (!Directory.Exists(groupFolder))
                                            destPath = Path.Combine(JsonBasedData.GetGroupsFolder(), lnkFileName);
                                    }

                                    File.Copy(lnkFile, destPath, true);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Error copying {Path.GetFileName(itemPath)}: {ex.Message}",
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }

                    // Refresh the items list
                    InitializeItems();
                }
        }
    
        private void renameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // For now, just show properties dialog instead of rename
            propertiesToolStripMenuItem_Click(sender, e);
        }

        #region Icon Size Management

        private void LoadIconSizeFromINI()
        {
            // Load saved icon size from application settings
            var iconSizeStr = JsonBasedData.LoadApplicationSetting("icon_size", "32");
            if (int.TryParse(iconSizeStr, out int savedSize))
            {
                // Validate against allowed sizes: 16, 32, 48, 64
                int[] allowedSizes = { 16, 32, 48, 64 };
                if (allowedSizes.Contains(savedSize))
                {
                    currentIconSize = savedSize;
                }
                else
                {
                    // Find closest allowed size
                    currentIconSize = allowedSizes.OrderBy(s => Math.Abs(s - savedSize)).First();
                }
            }
            else
            {
                currentIconSize = 32; // Default
            }

            // Update ImageList size
            imageListIcons.ImageSize = new Size(currentIconSize, currentIconSize);
        }

        private void SaveIconSizeToINI()
        {
            JsonBasedData.SaveApplicationSettings("icon_size", currentIconSize.ToString());
        }

        private void FormChild_MouseWheel(object sender, MouseEventArgs e)
        {
            // Check if CTRL key is pressed
            if (Control.ModifierKeys == Keys.Control)
            {
                // Fixed icon sizes: 16, 32, 48, 64
                int[] iconSizes = { 16, 32, 48, 64 };
                int currentIndex = Array.IndexOf(iconSizes, currentIconSize);

                // If current size is not in the array, find the closest one
                if (currentIndex == -1)
                {
                    currentIndex = 1; // Default to 32
                    for (int i = 0; i < iconSizes.Length; i++)
                    {
                        if (Math.Abs(iconSizes[i] - currentIconSize) < Math.Abs(iconSizes[currentIndex] - currentIconSize))
                        {
                            currentIndex = i;
                        }
                    }
                }

                // Calculate new index
                int newIndex = currentIndex;
                if (e.Delta > 0 && currentIndex < iconSizes.Length - 1)
                {
                    newIndex = currentIndex + 1; // Increase size
                }
                else if (e.Delta < 0 && currentIndex > 0)
                {
                    newIndex = currentIndex - 1; // Decrease size
                }

                int newSize = iconSizes[newIndex];

                if (newSize != currentIconSize)
                {
                    SetIconSize(newSize);
                }

                // Prevent the mouse wheel from scrolling the ListView
                ((HandledMouseEventArgs)e).Handled = true;
            }
        }

        public void SetIconSize(int newSize)
        {
            // Validate size is one of the allowed values
            int[] allowedSizes = { 16, 32, 48, 64 };
            if (!allowedSizes.Contains(newSize))
                return;

            currentIconSize = newSize;

            // Update ImageList size
            imageListIcons.ImageSize = new Size(currentIconSize, currentIconSize);

            // Save to INI
            SaveIconSizeToINI();

            // Refresh icons with new size
            RefreshIconsWithNewSize();

            // Notify main form to update all other windows
            var mainForm = this.MdiParent as FormMain;
            mainForm?.NotifyIconSizeChanged(this, newSize);
        }

        public int GetCurrentIconSize()
        {
            return currentIconSize;
        }

        /// <summary>
        /// Sets icon size without triggering notifications (used by main form to sync all windows)
        /// </summary>
        public void SetIconSizeSilent(int newSize)
        {
            // Validate size is one of the allowed values
            int[] allowedSizes = { 16, 32, 48, 64 };
            if (!allowedSizes.Contains(newSize))
                return;

            currentIconSize = newSize;

            // Update ImageList size
            imageListIcons.ImageSize = new Size(currentIconSize, currentIconSize);

            // Refresh icons with new size (but don't save or notify)
            RefreshIconsWithNewSize();
        }

        private void RefreshIconsWithNewSize()
        {
            // Store current shortcut data
            var groupName = GetGroupNameFromId(this.Tag?.ToString() ?? "");
            if (string.IsNullOrEmpty(groupName)) return;

            var shortcuts = JsonBasedData.GetShortcutsInGroupWithRoot(groupName);

            // Clear and rebuild the image list with new size
            imageListIcons.Images.Clear();
            listViewMain.Items.Clear();

            // Re-initialize items with new icon size
            for (int i = 0; i < shortcuts.Count; i++)
            {
                var shortcut = shortcuts[i];

                try
                {
                    // Try to load the icon
                    Icon extractedIcon = ExtractIconFromShortcut(shortcut);

                    // Add the icon to the image list with new size
                    if (extractedIcon != null)
                    {
                        var bitmap = new Bitmap(extractedIcon.ToBitmap(), new Size(currentIconSize, currentIconSize));
                        imageListIcons.Images.Add(bitmap);
                    }
                    else
                    {
                        var bitmap = new Bitmap(SystemIcons.Application.ToBitmap(), new Size(currentIconSize, currentIconSize));
                        imageListIcons.Images.Add(bitmap);
                    }

                    ListViewItem item = new ListViewItem();
                    item.Text = shortcut.Name;
                    item.ImageIndex = i;

                    // Build tooltip with target and arguments, showing environment variables if present
                    string tooltip = shortcut.TargetPath;

                    // Check if target contains environment variables and show both versions
                    if (JsonBasedData.ContainsEnvironmentVariables(shortcut.TargetPath))
                    {
                        string expandedPath = JsonBasedData.ExpandEnvironmentVariablesEnhanced(shortcut.TargetPath);
                        tooltip = $"{shortcut.TargetPath}\n→ {expandedPath}";
                    }
                    else
                    {
                        // Try the old method as fallback
                        string expandedPath = JsonBasedData.ExpandEnvironmentVariables(shortcut.TargetPath);
                        if (shortcut.TargetPath != expandedPath)
                        {
                            tooltip = $"{shortcut.TargetPath}\n→ {expandedPath}";
                        }
                    }

                    if (!string.IsNullOrEmpty(shortcut.Arguments))
                    {
                        if (JsonBasedData.ContainsEnvironmentVariables(shortcut.Arguments))
                        {
                            string expandedArgs = JsonBasedData.ExpandEnvironmentVariablesEnhanced(shortcut.Arguments);
                            tooltip += $"\nArguments: {shortcut.Arguments}\n→ {expandedArgs}";
                        }
                        else
                        {
                            string expandedArgs = JsonBasedData.ExpandEnvironmentVariables(shortcut.Arguments);
                            if (shortcut.Arguments != expandedArgs)
                            {
                                tooltip += $"\nArguments: {shortcut.Arguments}\n→ {expandedArgs}";
                            }
                            else
                            {
                                tooltip += $"\nArguments: {shortcut.Arguments}";
                            }
                        }
                    }

                    item.ToolTipText = tooltip;
                    item.Tag = shortcut;
                    listViewMain.Items.Add(item);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error processing shortcut {shortcut.Name}: {ex.Message}");

                    var bitmap = new Bitmap(SystemIcons.Error.ToBitmap(), new Size(currentIconSize, currentIconSize));
                    imageListIcons.Images.Add(bitmap);

                    ListViewItem item = new ListViewItem();
                    item.Text = shortcut.Name;
                    item.ImageIndex = i;
                    item.ToolTipText = shortcut.TargetPath + " (Error loading)";
                    item.Tag = shortcut;
                    listViewMain.Items.Add(item);
                }
            }
        }

        #endregion

        #region Form-Level Drag & Drop (for when dropping outside ListView)

        private void FormChild_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void FormChild_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                HandleExternalFileDrop(e);
            }
        }

        #endregion

        #region External Drag & Drop Support

        /// <summary>
        /// Handle files dropped from external applications (Explorer, etc.)
        /// </summary>
        private void HandleExternalFileDrop(DragEventArgs e)
        {
            try
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                var groupName = GetGroupNameFromId(this.Tag?.ToString() ?? "");
                var targetFolder = JsonBasedData.GetGroupsFolder();

                if (groupName != "Programs")
                {
                    targetFolder = Path.Combine(targetFolder, groupName);
                }

                foreach (string file in files)
                {
                    if (File.Exists(file))
                    {
                        string fileName = Path.GetFileName(file);
                        string fileExt = Path.GetExtension(file).ToLower();

                        if (fileExt == ".lnk")
                        {
                            // It's already a shortcut - copy it directly
                            string targetPath = Path.Combine(targetFolder, fileName);

                            // Make sure we don't overwrite existing shortcuts
                            targetPath = GetUniqueFileName(targetPath);

                            File.Copy(file, targetPath);
                        }
                        else if (fileExt == ".exe" || fileExt == ".bat" || fileExt == ".cmd" || 
                                fileExt == ".com" || fileExt == ".msi")
                        {
                            // Create a shortcut for executable files
                            string shortcutName = Path.GetFileNameWithoutExtension(file);
                            CreateShortcutFromFile(file, shortcutName, targetFolder);
                        }
                        else
                        {
                            // For other file types, create a shortcut that will open with default program
                            string shortcutName = Path.GetFileNameWithoutExtension(file);
                            CreateShortcutFromFile(file, shortcutName, targetFolder);
                        }
                    }
                    else if (Directory.Exists(file))
                    {
                        // Create a shortcut to the folder
                        string folderName = Path.GetFileName(file);
                        CreateShortcutFromFile(file, folderName, targetFolder);
                    }
                }

                // Refresh the view to show new shortcuts
                InitializeItems();

                // Notify other windows to refresh as well
                var mainForm = this.MdiParent as FormMain;
                mainForm?.RefreshAllChildWindows();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding shortcut: {ex.Message}", 
                    "Drop Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Create a shortcut from a dropped file
        /// </summary>
        private void CreateShortcutFromFile(string sourceFile, string shortcutName, string targetFolder)
        {
            try
            {
                // Ensure target folder exists
                if (!Directory.Exists(targetFolder))
                {
                    Directory.CreateDirectory(targetFolder);
                }

                string shortcutPath = Path.Combine(targetFolder, shortcutName + ".lnk");
                shortcutPath = GetUniqueFileName(shortcutPath);

                // Use the same method as FormCreateItem to create shortcuts
                var groupName = GetGroupNameFromId(this.Tag?.ToString() ?? "");
                JsonBasedData.CreateShortcut(groupName, shortcutName, sourceFile, "", "", 0);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating shortcut for {sourceFile}: {ex.Message}", 
                    "Shortcut Creation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// Get a unique filename by appending a number if needed
        /// </summary>
        private string GetUniqueFileName(string filePath)
        {
            if (!File.Exists(filePath))
                return filePath;

            string directory = Path.GetDirectoryName(filePath);
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            string extension = Path.GetExtension(filePath);

            int counter = 1;
            string newPath;

            do
            {
                newPath = Path.Combine(directory, $"{fileName} ({counter}){extension}");
                counter++;
            } while (File.Exists(newPath));

            return newPath;
        }

        #endregion
    }
}
