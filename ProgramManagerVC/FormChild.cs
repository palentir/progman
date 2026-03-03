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
            var currentProfile = FileBasedData.GetCurrentProfile();
            
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
            }
            
            previousWindowState = this.WindowState;
        }

        private void ListViewMain_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (listViewMain.SelectedItems.Count > 0)
            {
                var shortcutInfo = (ShortcutInfo)listViewMain.SelectedItems[0].Tag;
                
                // Expand environment variables before checking file existence
                string expandedTargetPath = FileBasedData.ExpandEnvironmentVariables(shortcutInfo.TargetPath);
                
                // Check if file exists before trying to launch it
                if (System.IO.File.Exists(expandedTargetPath))
                {
                    try
                    {
                        var psi = new ProcessStartInfo();
                        psi.FileName = expandedTargetPath;
                        psi.Arguments = FileBasedData.ExpandEnvironmentVariables(shortcutInfo.Arguments ?? "");
                        
                        string expandedWorkingDir = FileBasedData.ExpandEnvironmentVariables(shortcutInfo.WorkingDirectory ?? "");
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
            var shortcuts = FileBasedData.GetShortcutsInGroupWithRoot(groupName);
            
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
                    string expandedPath = FileBasedData.ExpandEnvironmentVariables(shortcut.TargetPath);
                    
                    // Show expanded path if it differs from original (contains environment variables)
                    if (shortcut.TargetPath != expandedPath)
                    {
                        tooltip = $"{shortcut.TargetPath}\n? {expandedPath}";
                    }
                    
                    if (!string.IsNullOrEmpty(shortcut.Arguments))
                    {
                        string expandedArgs = FileBasedData.ExpandEnvironmentVariables(shortcut.Arguments);
                        if (shortcut.Arguments != expandedArgs)
                        {
                            tooltip += $"\nArguments: {shortcut.Arguments}\n? {expandedArgs}";
                        }
                        else
                        {
                            tooltip += $"\nArguments: {shortcut.Arguments}";
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
                string expandedIconPath = FileBasedData.ExpandEnvironmentVariables(shortcut.IconLocation);
                if (File.Exists(expandedIconPath))
                {
                    extractedIcon = TryExtractIconFromFile(expandedIconPath, shortcut.IconIndex);
                    if (extractedIcon != null) return extractedIcon;
                }
            }

            // Strategy 2: Try to load icon from the target executable
            if (!string.IsNullOrEmpty(shortcut.TargetPath))
            {
                string expandedTargetPath = FileBasedData.ExpandEnvironmentVariables(shortcut.TargetPath);
                if (File.Exists(expandedTargetPath))
                {
                    extractedIcon = TryExtractIconFromFile(expandedTargetPath, 0);
                    if (extractedIcon != null) return extractedIcon;
                }
            }

            // Strategy 3: Try to get associated icon for the target file
            if (!string.IsNullOrEmpty(shortcut.TargetPath))
            {
                string expandedTargetPath = FileBasedData.ExpandEnvironmentVariables(shortcut.TargetPath);
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
                string expandedTargetPath = FileBasedData.ExpandEnvironmentVariables(shortcut.TargetPath);
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
                string expandedPath = FileBasedData.ExpandEnvironmentVariables(filePath);
                
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
            var groups = FileBasedData.GetAllGroups();
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
                var groups = FileBasedData.GetAllGroups();
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
                    FileBasedData.SaveGroupSettings(group);
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
                string expandedTargetPath = FileBasedData.ExpandEnvironmentVariables(shortcutInfo.TargetPath);
                
                // Check if file exists before trying to launch it
                if (System.IO.File.Exists(expandedTargetPath))
                {
                    try
                    {
                        var psi = new ProcessStartInfo();
                        psi.FileName = expandedTargetPath;
                        psi.Arguments = FileBasedData.ExpandEnvironmentVariables(shortcutInfo.Arguments ?? "");
                        
                        string expandedWorkingDir = FileBasedData.ExpandEnvironmentVariables(shortcutInfo.WorkingDirectory ?? "");
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
                string expandedTargetPath = FileBasedData.ExpandEnvironmentVariables(shortcutInfo.TargetPath);
                
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
                    string expandedTargetPath = FileBasedData.ExpandEnvironmentVariables(shortcutInfo.TargetPath);
                    
                    // Check if file exists before trying to run it
                    if (System.IO.File.Exists(expandedTargetPath))
                    {
                        try 
                        {
                            Process proc = new Process();
                            proc.StartInfo.FileName = expandedTargetPath;
                            proc.StartInfo.Arguments = FileBasedData.ExpandEnvironmentVariables(shortcutInfo.Arguments ?? "");
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
                        var shortcuts = FileBasedData.GetShortcutsInGroupWithRoot(groupName);
                        for (int i = 0; i < shortcuts.Count; i++)
                        {
                            shortcuts[i].DisplayOrder = i;
                        }
                        FileBasedData.SaveShortcutDisplayOrder(shortcuts, groupName);

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
                var groups = FileBasedData.GetAllGroups();
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
                var profilePath = FileBasedData.GetGroupsFolder();
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
                        FileBasedData.DeleteGroup(groupName);
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
                // Handle external file drop (existing functionality)
                HandleFileDrop(e);
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
                var shortcuts = FileBasedData.GetShortcutsInGroupWithRoot(groupName);

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
                    FileBasedData.SaveShortcutDisplayOrder(shortcuts, groupName);

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
                                    var destPath = Path.Combine(FileBasedData.GetGroupsFolder(), groupName, fileName);
                                    if (groupName == "Programs")
                                    {
                                        // For Programs group, copy to root if no subfolder exists
                                        var groupFolder = Path.Combine(FileBasedData.GetGroupsFolder(), groupName);
                                        if (!Directory.Exists(groupFolder))
                                            destPath = Path.Combine(FileBasedData.GetGroupsFolder(), fileName);
                                    }

                                    File.Copy(itemPath, destPath, true);
                                }
                                else
                                {
                                    // Create shortcut for the file
                                    string shortcutName = Path.GetFileNameWithoutExtension(fileName);
                                    FileBasedData.CreateShortcut(groupName, shortcutName, itemPath, "", itemPath, 0);
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
                                    var destPath = Path.Combine(FileBasedData.GetGroupsFolder(), groupName, lnkFileName);
                                    if (groupName == "Programs")
                                    {
                                        var groupFolder = Path.Combine(FileBasedData.GetGroupsFolder(), groupName);
                                        if (!Directory.Exists(groupFolder))
                                            destPath = Path.Combine(FileBasedData.GetGroupsFolder(), lnkFileName);
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
            var iconSizeStr = FileBasedData.LoadApplicationSetting("icon_size", "32");
            if (int.TryParse(iconSizeStr, out int savedSize))
            {
                // Clamp to valid range (16 to 128)
                currentIconSize = Math.Max(16, Math.Min(128, savedSize));
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
            FileBasedData.SaveApplicationSettings("icon_size", currentIconSize.ToString());
        }

        private void FormChild_MouseWheel(object sender, MouseEventArgs e)
        {
            // Check if CTRL key is pressed
            if (Control.ModifierKeys == Keys.Control)
            {
                // Calculate new size
                int delta = e.Delta > 0 ? 4 : -4; // Increase/decrease by 4 pixels
                int newSize = currentIconSize + delta;

                // Clamp to valid range (16 to 128)
                newSize = Math.Max(16, Math.Min(128, newSize));

                if (newSize != currentIconSize)
                {
                    currentIconSize = newSize;

                    // Update ImageList size
                    imageListIcons.ImageSize = new Size(currentIconSize, currentIconSize);

                    // Save to INI
                    SaveIconSizeToINI();

                    // Refresh icons with new size
                    RefreshIconsWithNewSize();
                }

                // Prevent the mouse wheel from scrolling the ListView
                ((HandledMouseEventArgs)e).Handled = true;
            }
        }

        private void RefreshIconsWithNewSize()
        {
            // Store current shortcut data
            var groupName = GetGroupNameFromId(this.Tag?.ToString() ?? "");
            if (string.IsNullOrEmpty(groupName)) return;

            var shortcuts = FileBasedData.GetShortcutsInGroupWithRoot(groupName);

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
                    string expandedPath = FileBasedData.ExpandEnvironmentVariables(shortcut.TargetPath);

                    // Show expanded path if it differs from original (contains environment variables)
                    if (shortcut.TargetPath != expandedPath)
                    {
                        tooltip = $"{shortcut.TargetPath}\n→ {expandedPath}";
                    }

                    if (!string.IsNullOrEmpty(shortcut.Arguments))
                    {
                        string expandedArgs = FileBasedData.ExpandEnvironmentVariables(shortcut.Arguments);
                        if (shortcut.Arguments != expandedArgs)
                        {
                            tooltip += $"\nArguments: {shortcut.Arguments}\n→ {expandedArgs}";
                        }
                        else
                        {
                            tooltip += $"\nArguments: {shortcut.Arguments}";
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
    }
}
