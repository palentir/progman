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
            InitializeItems();
            if (System.Environment.OSVersion.Version.Major < 6) {
                runAsAdministratorToolStripMenuItem.Visible = false;
            } else {
                runAsAdministratorToolStripMenuItem.Image = SystemIcons.Shield.ToBitmap();
            }
            
            previousWindowState = this.WindowState;
            this.Resize += FormChild_Resize;
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
                
                // Check if file exists before trying to launch it
                if (System.IO.File.Exists(shortcutInfo.TargetPath))
                {
                    try
                    {
                        var psi = new ProcessStartInfo();
                        psi.FileName = shortcutInfo.TargetPath;
                        psi.Arguments = shortcutInfo.Arguments ?? "";
                        psi.WorkingDirectory = shortcutInfo.WorkingDirectory ?? Path.GetDirectoryName(shortcutInfo.TargetPath);
                        psi.UseShellExecute = true;
                        Process.Start(psi);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Could not open file:\n\n" + shortcutInfo.TargetPath + 
                                       "\n\nError: " + ex.Message, 
                                       "Error Opening File", 
                                       MessageBoxButtons.OK, 
                                       MessageBoxIcon.Error);
                    }
                }
                else
                {
                    MessageBox.Show("The file does not exist:\n\n" + shortcutInfo.TargetPath + 
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
                        imageListIcons.Images.Add(extractedIcon.ToBitmap());
                    }
                    else
                    {
                        // Ultimate fallback - use a default application icon
                        imageListIcons.Images.Add(SystemIcons.Application.ToBitmap());
                    }
                    
                    ListViewItem item = new ListViewItem();
                    item.Text = shortcut.Name;
                    item.ImageIndex = i;
                    
                    // Build tooltip with target and arguments
                    string tooltip = shortcut.TargetPath;
                    if (!string.IsNullOrEmpty(shortcut.Arguments))
                        tooltip += " " + shortcut.Arguments;
                    item.ToolTipText = tooltip;
                    
                    item.Tag = shortcut; // Store the entire shortcut info
                    listViewMain.Items.Add(item);
                }
                catch (Exception ex)
                {
                    // Even if there's an exception, still show the item
                    System.Diagnostics.Debug.WriteLine($"Error processing shortcut {shortcut.Name}: {ex.Message}");
                    
                    imageListIcons.Images.Add(SystemIcons.Error.ToBitmap());
                    
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
            if (!string.IsNullOrEmpty(shortcut.IconLocation) && File.Exists(shortcut.IconLocation))
            {
                extractedIcon = TryExtractIconFromFile(shortcut.IconLocation, shortcut.IconIndex);
                if (extractedIcon != null) return extractedIcon;
            }

            // Strategy 2: Try to load icon from the target executable
            if (!string.IsNullOrEmpty(shortcut.TargetPath) && File.Exists(shortcut.TargetPath))
            {
                extractedIcon = TryExtractIconFromFile(shortcut.TargetPath, 0);
                if (extractedIcon != null) return extractedIcon;
            }

            // Strategy 3: Try to get associated icon for the target file
            if (!string.IsNullOrEmpty(shortcut.TargetPath) && File.Exists(shortcut.TargetPath))
            {
                try
                {
                    extractedIcon = Icon.ExtractAssociatedIcon(shortcut.TargetPath);
                    if (extractedIcon != null) return extractedIcon;
                }
                catch { }
            }

            // Strategy 4: Try to get associated icon for the shortcut file itself
            try
            {
                extractedIcon = Icon.ExtractAssociatedIcon(shortcut.ShortcutPath);
                if (extractedIcon != null) return extractedIcon;
            }
            catch { }

            // Strategy 5: Use system warning icon for missing files
            if (!File.Exists(shortcut.TargetPath))
            {
                return SystemIcons.Warning;
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
                if (!File.Exists(filePath)) return null;

                string extension = Path.GetExtension(filePath).ToLower();
                
                if (extension == ".ico")
                {
                    return new Icon(filePath);
                }
                else if (extension == ".exe" || extension == ".dll")
                {
                    // Extract specific icon by index using Windows Shell API
                    IntPtr hIcon = ExtractIcon(IntPtr.Zero, filePath, iconIndex);
                    if (hIcon != IntPtr.Zero && hIcon != (IntPtr)1)
                    {
                        Icon icon = Icon.FromHandle(hIcon);
                        // Don't destroy the icon handle here, let GC handle it
                        return icon;
                    }
                }
                
                // Fallback to associated icon
                return Icon.ExtractAssociatedIcon(filePath);
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
                
                // Check if file exists before trying to launch it
                if (System.IO.File.Exists(shortcutInfo.TargetPath))
                {
                    try
                    {
                        var psi = new ProcessStartInfo();
                        psi.FileName = shortcutInfo.TargetPath;
                        psi.Arguments = shortcutInfo.Arguments ?? "";
                        psi.WorkingDirectory = shortcutInfo.WorkingDirectory ?? Path.GetDirectoryName(shortcutInfo.TargetPath);
                        psi.UseShellExecute = true;
                        Process.Start(psi);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Could not open file:\n\n" + shortcutInfo.TargetPath + 
                                       "\n\nError: " + ex.Message, 
                                       "Error Opening File", 
                                       MessageBoxButtons.OK, 
                                       MessageBoxIcon.Error);
                    }
                }
                else
                {
                    MessageBox.Show("The file does not exist:\n\n" + shortcutInfo.TargetPath + 
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
                
                // Check if file exists before trying to show it in explorer
                if (System.IO.File.Exists(shortcutInfo.TargetPath))
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo("explorer.exe", "/select, \"" + shortcutInfo.TargetPath + "\""));
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Could not open file location:\n\n" + shortcutInfo.TargetPath + 
                                       "\n\nError: " + ex.Message, 
                                       "Error", 
                                       MessageBoxButtons.OK, 
                                       MessageBoxIcon.Error);
                    }
                }
                else
                {
                    MessageBox.Show("The file does not exist:\n\n" + shortcutInfo.TargetPath + 
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
                    
                    // Check if file exists before trying to run it
                    if (System.IO.File.Exists(shortcutInfo.TargetPath))
                    {
                        try 
                        {
                            Process proc = new Process();
                            proc.StartInfo.FileName = shortcutInfo.TargetPath;
                            proc.StartInfo.Arguments = shortcutInfo.Arguments ?? "";
                            proc.StartInfo.UseShellExecute = true;
                            proc.StartInfo.Verb = "runas";
                            proc.Start();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Could not run file as administrator:\n\n" + shortcutInfo.TargetPath + 
                                           "\n\nError: " + ex.Message, 
                                           "Error", 
                                           MessageBoxButtons.OK, 
                                           MessageBoxIcon.Error);
                        }
                    }
                    else
                    {
                        MessageBox.Show("The file does not exist:\n\n" + shortcutInfo.TargetPath + 
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
                if (MessageBox.Show("Do you really want to delete the \"" + selectedItem.Text + "\" item?",
                                       "Confirm",
                                       MessageBoxButtons.YesNo,
                                       MessageBoxIcon.Question) == DialogResult.Yes) 
                {
                    var groupName = GetGroupNameFromId(this.Tag?.ToString() ?? "");
                    var shortcutInfo = (ShortcutInfo)selectedItem.Tag;
                    
                    // Delete the .lnk file
                    if (File.Exists(shortcutInfo.ShortcutPath))
                    {
                        File.Delete(shortcutInfo.ShortcutPath);
                    }
                    
                    InitializeItems();
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
            if (MessageBox.Show("Do you really want to delete \"" + this.Text + "\" group?",
                               "Confirm",
                               MessageBoxButtons.YesNo,
                               MessageBoxIcon.Question) == DialogResult.Yes)
            {
                var groupName = GetGroupNameFromId(this.Tag?.ToString() ?? "");
                FileBasedData.DeleteGroup(groupName);
                this.Hide();
                this.DestroyHandle();
            }
        }
    }
}
