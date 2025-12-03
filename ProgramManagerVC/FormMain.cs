using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace ProgramManagerVC
{
    public partial class FormMain : Form
    {
        private List<MinimizedIcon> minimizedIcons = new List<MinimizedIcon>();
        private IconHostForm iconHost;
        private MinimizedIcon selectedIcon; // track selected icon

        // Use full client area for hosting icons (no separate bottom bar)
        private Control GetIconHost()
        {
            if (iconHost == null || iconHost.IsDisposed)
            {
                iconHost = new IconHostForm();
                iconHost.MdiParent = this;
                iconHost.FormBorderStyle = FormBorderStyle.None;
                iconHost.ShowInTaskbar = false;
                iconHost.ControlBox = false;
                iconHost.MinimizeBox = false;
                iconHost.MaximizeBox = false;
                iconHost.StartPosition = FormStartPosition.Manual;
                iconHost.BackColor = SystemColors.Window;
                iconHost.Dock = DockStyle.Fill; // fill MDI client to avoid top gray patch
                iconHost.Show();
                iconHost.SendToBack();
            }
            EnsureIconHostLayout();
            return iconHost;
        }

        private void EnsureIconHostLayout()
        {
            if (iconHost != null && !iconHost.IsDisposed)
            {
                // Ensure full fill and location within MDI client coordinates
                iconHost.Dock = DockStyle.Fill;
                iconHost.Location = new Point(0, 0);
            }
        }

        public FormMain()
        {
            InitializeComponent();
            this.MdiChildActivate += FormMain_MdiChildActivate;
            this.Resize += FormMain_Resize;
        }

        private void NewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form createForm = new FormCreateGroup();
            if (createForm.ShowDialog() == DialogResult.OK)
            {
                CloseAllMDIWindows();
                InitializeMDI();
            } 
        }

        private void FormMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = false;
            if (e.CloseReason == CloseReason.UserClosing)
            {
                DialogResult result = MessageBox.Show("This program will be closed.", "Confirm", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
                if (result == DialogResult.Cancel)
                {
                    e.Cancel = true;
                }
                else
                {
                    SaveActiveWindow();
                    SaveWindowSizeAndPosition();
                }
            }
            else if (e.CloseReason == CloseReason.ApplicationExitCall)
            {
                SaveActiveWindow();
                SaveWindowSizeAndPosition();
                e.Cancel = false;
            }
        }

        private void FormMain_Load(object sender, EventArgs e)
        {
            InitializeMDI();
            InitializeTitle();
            LoadWindowSizeAndPosition();
        }

        private void InitializeTitle()
        {
            if (Properties.Settings.Default.UsernameInTitle == 1)
            {
                // Show only username without domain/workgroup
                Text = $"Program Manager - {Environment.UserName}";
            }
            else
            {
                Text = "Program Manager";
            }
        }

        private void SaveActiveWindow()
        {
            // Save which window was active when app closes
            var activeChild = this.ActiveMdiChild as FormChild;
            if (activeChild != null && activeChild.Tag != null)
            {
                FileBasedData.SaveApplicationSettings("active_window", activeChild.Tag.ToString());
            }
        }

        private void InitializeMDI()
        {
            try
            {
                // Load the current profile or default to Main
                var currentProfile = FileBasedData.GetCurrentProfile();
                var profiles = FileBasedData.GetAllProfiles();
                
                // Ensure we have at least the Main profile
                if (profiles == null || profiles.Count == 0)
                {
                    // Create default Main profile if no profiles exist
                    var mainGroupsPath = Path.Combine(Application.StartupPath, "Groups");
                    if (!Directory.Exists(mainGroupsPath))
                        Directory.CreateDirectory(mainGroupsPath);
                    
                    // Create Main profile
                    FileBasedData.SaveProfile("Main", mainGroupsPath);
                    FileBasedData.SetCurrentProfile("Main");
                    
                    // Reload profiles
                    profiles = FileBasedData.GetAllProfiles();
                    currentProfile = "Main";
                }
                
                // Find active profile, default to first available if current not found
                var activeProfile = profiles.FirstOrDefault(p => p.Name == currentProfile);
                if (activeProfile == null)
                {
                    activeProfile = profiles.FirstOrDefault();
                    if (activeProfile == null)
                    {
                        // This should not happen, but handle it gracefully
                        MessageBox.Show("No profiles available. Creating default Main profile.", 
                            "Profile Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    // Update current profile to the fallback
                    FileBasedData.SetCurrentProfile(activeProfile.Name);
                }
                
                // Set the groups folder based on active profile
                FileBasedData.SetGroupsFolder(activeProfile.Path);

                // Load all groups from folder structure
                var groups = FileBasedData.GetAllGroups();
                
                // Get last active window
                string lastActiveWindowId = FileBasedData.LoadApplicationSetting("active_window", "");
                FormChild windowToActivate = null;
                
                if (groups.Count > 0)
                {
                    foreach (var groupInfo in groups)
                    {
                        FormChild child = new FormChild();
                        child.Text = groupInfo.Name;
                        child.Tag = groupInfo.Id;
                        child.MdiParent = this;
                        
                        // Set window position and size from INI file
                        if (groupInfo.Width > 0 && groupInfo.Height > 0)
                        {
                            child.StartPosition = FormStartPosition.Manual;
                            child.Location = new Point(Math.Max(0, groupInfo.X), Math.Max(0, groupInfo.Y));
                            child.Size = new Size(Math.Max(300, groupInfo.Width), Math.Max(250, groupInfo.Height));
                        }
                        else
                        {
                            // Default size and position
                            child.Size = new Size(400, 300);
                            child.StartPosition = FormStartPosition.WindowsDefaultLocation;
                        }
                        
                        // Track which window should be activated last
                        if (groupInfo.Id == lastActiveWindowId)
                        {
                            windowToActivate = child;
                        }
                        
                        // Set window state
                        switch (groupInfo.WindowStatus)
                        {
                            case 0: // Minimized
                                child.Show();
                                child.WindowState = FormWindowState.Minimized;
                                break;
                            case 1: // Normal
                                child.Show();
                                child.WindowState = FormWindowState.Normal;
                                break;
                            case 2: // Maximized
                                child.Show();
                                this.BeginInvoke(new Action(() =>
                                {
                                    child.WindowState = FormWindowState.Maximized;
                                }));
                                break;
                            default:
                                child.Show();
                                child.WindowState = FormWindowState.Normal;
                                break;
                        }
                    }
                    
                    ArrangeMinimizedIcons();
                    
                    // Activate the previously active window last
                    if (windowToActivate != null && windowToActivate.WindowState != FormWindowState.Minimized)
                    {
                        this.BeginInvoke(new Action(() =>
                        {
                            windowToActivate.BringToFront();
                            windowToActivate.Activate();
                        }));
                    }
                }
                else
                {
                    // Create default Programs group structure
                    try
                    {
                        // Ensure the groups folder exists
                        if (!Directory.Exists(activeProfile.Path))
                            Directory.CreateDirectory(activeProfile.Path);
                            
                        FileBasedData.CreateGroup("Programs");
                        
                        // Create a sample shortcut
                        var notepadPath = Path.Combine(Environment.SystemDirectory, "notepad.exe");
                        if (File.Exists(notepadPath))
                        {
                            FileBasedData.CreateShortcut("Programs", "Notepad", notepadPath, "", notepadPath, 0);
                        }
                        
                        // Reload to show the new group
                        this.BeginInvoke(new Action(InitializeMDI));
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error creating default group: " + ex.Message, "Initialization Error", 
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                
                // Update window title to show current profile
                this.Text = $"Program Manager - {activeProfile.Name}";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error initializing application: " + ex.Message, "Initialization Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CloseAllMDIWindows()
        {
            // Save window states before closing
            foreach (Form frm in this.MdiChildren)
            {
                if (frm is FormChild childForm)
                {
                    SaveChildWindowState(childForm);
                }
                frm.Visible = false;
                frm.Dispose();
            }
        }

        private void SaveChildWindowState(FormChild childForm)
        {
            if (childForm.Tag != null)
            {
                // Find the corresponding group
                var groups = FileBasedData.GetAllGroups();
                var group = groups.FirstOrDefault(g => g.Id == childForm.Tag.ToString());
                
                if (group != null)
                {
                    // Update group info with current window state
                    group.Name = childForm.Text;
                    
                    if (childForm.WindowState == FormWindowState.Minimized)
                        group.WindowStatus = 0;
                    else if (childForm.WindowState == FormWindowState.Maximized)
                        group.WindowStatus = 2;
                    else
                        group.WindowStatus = 1;

                    if (childForm.WindowState == FormWindowState.Normal)
                    {
                        group.X = childForm.Location.X;
                        group.Y = childForm.Location.Y;
                        group.Width = childForm.Width;
                        group.Height = childForm.Height;
                    }
                    
                    // Save to INI file
                    FileBasedData.SaveGroupSettings(group);
                }
            }
        }

        private void NewItemToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.ActiveMdiChild is FormChild activeChild)
            {
                using (FormCreateItem createform = new FormCreateItem(activeChild.Tag.ToString()))
                {
                    if (createform.ShowDialog() == DialogResult.OK)
                    {
                        activeChild.InitializeItems();
                    }
                }
            }
        }

        private void DeleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.ActiveMdiChild is FormChild activeChild)
            {
                if (activeChild.listViewMain.SelectedItems.Count > 0)
                {
                    var selectedItem = activeChild.listViewMain.SelectedItems[0];
                    if (MessageBox.Show("Do you really want to delete the \"" + selectedItem.Text + "\" item?",
                                       "Confirm",
                                       MessageBoxButtons.YesNo,
                                       MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        // Delete the .lnk file
                        var groupName = GetGroupNameFromId(activeChild.Tag.ToString());
                        var shortcutFileName = selectedItem.Text + ".lnk";
                        FileBasedData.DeleteShortcut(groupName, shortcutFileName);
                        activeChild.InitializeItems();
                    }
                }
                else
                {
                    if (MessageBox.Show("Do you really want to delete \"" + activeChild.Text + "\" group?",
                                   "Confirm",
                                   MessageBoxButtons.YesNo,
                                   MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        var groupName = GetGroupNameFromId(activeChild.Tag.ToString());
                        FileBasedData.DeleteGroup(groupName);
                        activeChild.Hide();
                    }
                }
            }
        }

        private string GetGroupNameFromId(string id)
        {
            var groups = FileBasedData.GetAllGroups();
            var group = groups.FirstOrDefault(g => g.Id == id);
            return group?.Name ?? "";
        }

        private void AboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FormAbout about = new FormAbout();
            about.ShowDialog();
        }

        private void TileVerticalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LayoutMdi(MdiLayout.TileVertical);
        }

        private void TileHorizontalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LayoutMdi(MdiLayout.TileHorizontal);
        }

        private void CascadeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LayoutMdi(MdiLayout.Cascade);
        }

        private void ExecuteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FormExecute ex = new FormExecute();
            ex.ShowDialog();
        }

        private void PropertiesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.ActiveMdiChild is FormChild activeChild)
            {
                if (activeChild.listViewMain.SelectedItems.Count > 0)
                {
                    var selectedItem = activeChild.listViewMain.SelectedItems[0];
                    using (FormCreateItem createform = new FormCreateItem(activeChild.Tag.ToString(), selectedItem.Text))
                    {
                        if (createform.ShowDialog() == DialogResult.OK)
                        {
                            activeChild.InitializeItems();
                        }
                    }
                }
                else
                {
                    Form createForm = new FormCreateGroup(activeChild.Tag.ToString());
                    if (createForm.ShowDialog() == DialogResult.OK)
                    {
                        CloseAllMDIWindows();
                        InitializeMDI();
                    }
                }
            }
        }

        private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("This program will be closed.", "Confirm", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
            if (result == DialogResult.OK)
            {
                Environment.Exit(0);
            }
        }

        private void fileToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
        {
            var child = this.ActiveMdiChild as FormChild;
            bool hasChild = child != null;

            newItemToolStripMenuItem.Enabled = hasChild;
            deleteToolStripMenuItem.Enabled = hasChild;
            propertiesToolStripMenuItem.Enabled = hasChild;
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form formSettings = new FormSettings();
            formSettings.ShowDialog();
            if(formSettings.DialogResult == DialogResult.OK)
            InitializeTitle();
        }

        private void windowsToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            windowsToolStripMenuItem.DropDownItems.Clear();
            windowsToolStripMenuItem.DropDownItems.Add(tileVerticalToolStripMenuItem);
            windowsToolStripMenuItem.DropDownItems.Add(tileHorizontalToolStripMenuItem);
            windowsToolStripMenuItem.DropDownItems.Add(cascadeToolStripMenuItem);

            // Get only FormChild windows (exclude IconHostForm)
            var childWindows = this.MdiChildren.OfType<FormChild>().ToList();
            
            if (childWindows.Count > 0)
            {
                windowsToolStripMenuItem.DropDownItems.Add(new ToolStripSeparator());
            }

            // Add numbered menu items for each window
            for (int i = 0; i < childWindows.Count; i++)
            {
                FormChild child = childWindows[i];
                int windowNumber = i + 1;
                
                ToolStripMenuItem item = new ToolStripMenuItem($"{windowNumber} {child.Text}", null, (s, a) =>
                {
                    child.BringToFront();
                    child.Activate();
                });
                
                // Add checkmark to currently active window (only if not minimized)
                item.Checked = (child == this.ActiveMdiChild && child.WindowState != FormWindowState.Minimized);
                
                windowsToolStripMenuItem.DropDownItems.Add(item);
            }
        }

        private void LoadWindowSizeAndPosition()
        {
            var widthStr = FileBasedData.LoadApplicationSetting("window_width");
            var heightStr = FileBasedData.LoadApplicationSetting("window_height");
            var xStr = FileBasedData.LoadApplicationSetting("window_x");
            var yStr = FileBasedData.LoadApplicationSetting("window_y");
            
            if (int.TryParse(widthStr, out int width) && int.TryParse(heightStr, out int height) && width > 0 && height > 0)
            {
                this.Width = width;
                this.Height = height;
            }
            
            if (int.TryParse(xStr, out int x) && int.TryParse(yStr, out int y) && x >= 0 && y >= 0)
            {
                if (IsLocationOnScreen(x, y, this.Width, this.Height))
                {
                    this.StartPosition = FormStartPosition.Manual;
                    this.Location = new Point(x, y);
                }
            }
        }

        private bool IsLocationOnScreen(int x, int y, int width, int height)
        {
            Rectangle windowRect = new Rectangle(x, y, width, height);
            
            foreach (Screen screen in Screen.AllScreens)
            {
                if (screen.WorkingArea.IntersectsWith(windowRect))
                {
                    Rectangle intersection = Rectangle.Intersect(screen.WorkingArea, windowRect);
                    if (intersection.Width >= 100 && intersection.Height >= 100)
                    {
                        return true;
                    }
                }
            }
            
            return false;
        }

        private void SaveWindowSizeAndPosition()
        {
            if (this.WindowState == FormWindowState.Normal)
            {
                FileBasedData.SaveApplicationSettings("window_width", this.Width.ToString());
                FileBasedData.SaveApplicationSettings("window_height", this.Height.ToString());
                FileBasedData.SaveApplicationSettings("window_x", this.Location.X.ToString());
                FileBasedData.SaveApplicationSettings("window_y", this.Location.Y.ToString());
            }
        }

        private void FormMain_MdiChildActivate(object sender, EventArgs e)
        {
            // Only send IconHostForm to back when it gets activated (clicked)
            if (this.ActiveMdiChild is IconHostForm)
            {
                this.ActiveMdiChild.SendToBack();
            }
        }

        private void FormMain_Resize(object sender, EventArgs e)
        {
            EnsureIconHostLayout();
            ArrangeMinimizedIcons();
        }

        // Rest of the minimized icon management code remains the same...
        public void ArrangeMinimizedIcons()
        {
            var host = GetIconHost();

            foreach (MinimizedIcon icon in minimizedIcons.ToList())
            {
                if (icon.AssociatedForm == null || icon.AssociatedForm.IsDisposed)
                {
                    if (icon.Parent != null) icon.Parent.Controls.Remove(icon);
                    minimizedIcons.Remove(icon);
                    icon.Dispose();
                }
            }

            int spacing = 8;
            int iconWidth = 64;
            int iconHeight = minimizedIcons.Count > 0 ? minimizedIcons[0].Height : 64;

            int availableWidth = Math.Max(0, host.ClientSize.Width - (spacing * 2));
            int iconsPerRow = Math.Max(1, availableWidth / (iconWidth + spacing));

            int totalIcons = minimizedIcons.Count;
            int rows = Math.Max(1, (int)Math.Ceiling(totalIcons / (double)iconsPerRow));
            int totalHeightNeeded = rows * (iconHeight + spacing) + spacing;

            int startX = spacing;
            int startY = host.ClientSize.Height - totalHeightNeeded;
            if (startY < spacing)
            {
                startY = spacing;
            }

            int currentX = startX;
            int currentY = startY;
            int rowCount = 0;
            int placedInRow = 0;

            for (int i = 0; i < minimizedIcons.Count; i++)
            {
                MinimizedIcon icon = minimizedIcons[i];
                if (icon.AssociatedForm != null && !icon.AssociatedForm.IsDisposed)
                {
                    if (icon.Parent != host)
                    {
                        icon.Parent?.Controls.Remove(icon);
                        host.Controls.Add(icon);
                    }

                    icon.Location = new Point(currentX, currentY);
                    placedInRow++;

                    if (placedInRow >= iconsPerRow)
                    {
                        rowCount++;
                        placedInRow = 0;
                        currentX = startX;
                        currentY += iconHeight + spacing;
                        if (currentY + iconHeight > host.ClientSize.Height - spacing)
                        {
                            currentY = Math.Max(spacing, host.ClientSize.Height - iconHeight - spacing);
                        }
                    }
                    else
                    {
                        currentX += iconWidth + spacing;
                    }
                }
            }
        }

        public void AddMinimizedIcon(Form form)
        {
            MinimizedIcon existingIcon = minimizedIcons.FirstOrDefault(i => i.AssociatedForm == form);
            if (existingIcon == null)
            {
                MinimizedIcon icon = new MinimizedIcon(form, this);
                icon.Font = new Font("MS Sans Serif", 8F);
                minimizedIcons.Add(icon);

                var host = GetIconHost();
                host.Controls.Add(icon);
                ArrangeMinimizedIcons();
            }
        }

        public void SetSelectedIcon(MinimizedIcon icon)
        {
            if (selectedIcon != null && selectedIcon != icon)
            {
                selectedIcon.Selected = false;
            }
            selectedIcon = icon;
            if (selectedIcon != null)
            {
                selectedIcon.Selected = true;
            }
        }

        public void RemoveMinimizedIcon(Form form)
        {
            MinimizedIcon icon = minimizedIcons.FirstOrDefault(i => i.AssociatedForm == form);
            if (icon != null)
            {
                if (icon == selectedIcon)
                {
                    selectedIcon = null;
                }

                icon.Parent?.Controls.Remove(icon);
                minimizedIcons.Remove(icon);
                icon.Dispose();
                ArrangeMinimizedIcons();
            }
        }

        public void SwapIconPositions(MinimizedIcon icon1, MinimizedIcon icon2)
        {
            int index1 = minimizedIcons.IndexOf(icon1);
            int index2 = minimizedIcons.IndexOf(icon2);

            if (index1 >= 0 && index2 >= 0 && index1 != index2)
            {
                minimizedIcons[index1] = icon2;
                minimizedIcons[index2] = icon1;
                ArrangeMinimizedIcons();
            }
        }

        #region Profile Menu Handlers

        private void profileToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            // Clear existing profile items (keep Main, separator, Add Profile, Delete Profile)
            var itemsToRemove = new List<ToolStripItem>();
            for (int i = 0; i < profileToolStripMenuItem.DropDownItems.Count; i++)
            {
                var item = profileToolStripMenuItem.DropDownItems[i];
                if (item != mainProfileToolStripMenuItem && 
                    item != toolStripMenuItem3 && 
                    item != addProfileToolStripMenuItem && 
                    item != deleteProfileToolStripMenuItem)
                {
                    itemsToRemove.Add(item);
                }
            }
            
            foreach (var item in itemsToRemove)
            {
                profileToolStripMenuItem.DropDownItems.Remove(item);
            }
            
            // Add custom profiles
            var profiles = FileBasedData.GetAllProfiles();
            var currentProfile = FileBasedData.GetCurrentProfile();
            
            // Mark current profile
            mainProfileToolStripMenuItem.Checked = (currentProfile == "Main");
            
            // Insert custom profiles after Main but before separator
            int insertIndex = 1; // After Main
            
            foreach (var profile in profiles.Where(p => p.Name != "Main"))
            {
                var profileItem = new ToolStripMenuItem(profile.Name);
                profileItem.Checked = (currentProfile == profile.Name);
                profileItem.Click += (s, evt) => LoadProfile(profile.Name);
                profileToolStripMenuItem.DropDownItems.Insert(insertIndex++, profileItem);
            }
            
            // Enable/disable Delete Profile based on current selection
            deleteProfileToolStripMenuItem.Enabled = (currentProfile != "Main");
        }

        private void mainProfileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LoadProfile("Main");
        }

        private void addProfileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DialogResult result = folderBrowserDialogProfile.ShowDialog();
            if (result == DialogResult.OK)
            {
                string selectedPath = folderBrowserDialogProfile.SelectedPath;
                
                // Ask for profile name using a simple input form
                string profileName = ShowInputDialog("Enter a name for this profile:", 
                    "Profile Name", Path.GetFileName(selectedPath));
                
                if (!string.IsNullOrEmpty(profileName))
                {
                    try
                    {
                        // Save the profile
                        FileBasedData.SaveProfile(profileName, selectedPath);
                        
                        // Switch to the new profile
                        LoadProfile(profileName);
                        
                        MessageBox.Show($"Profile '{profileName}' added successfully!", 
                            "Profile Added", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error adding profile: {ex.Message}", 
                            "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private string ShowInputDialog(string prompt, string title, string defaultValue = "")
        {
            Form inputForm = new Form()
            {
                Width = 400,
                Height = 150,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = title,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false
            };
            
            Label lblPrompt = new Label() { Left = 15, Top = 20, Text = prompt, Width = 350 };
            TextBox txtInput = new TextBox() { Left = 15, Top = 45, Width = 350, Text = defaultValue };
            Button btnOK = new Button() { Text = "OK", Left = 235, Width = 60, Top = 75, DialogResult = DialogResult.OK };
            Button btnCancel = new Button() { Text = "Cancel", Left = 305, Width = 60, Top = 75, DialogResult = DialogResult.Cancel };
            
            inputForm.Controls.Add(lblPrompt);
            inputForm.Controls.Add(txtInput);
            inputForm.Controls.Add(btnOK);
            inputForm.Controls.Add(btnCancel);
            inputForm.AcceptButton = btnOK;
            inputForm.CancelButton = btnCancel;
            
            return inputForm.ShowDialog(this) == DialogResult.OK ? txtInput.Text : "";
        }

        private void deleteProfileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var currentProfile = FileBasedData.GetCurrentProfile();
            
            if (currentProfile == "Main")
            {
                MessageBox.Show("Cannot delete the Main profile.", 
                    "Cannot Delete", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            if (MessageBox.Show($"Are you sure you want to delete the '{currentProfile}' profile?\n\nThis will not delete the actual files, only the profile reference.", 
                "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                try
                {
                    FileBasedData.DeleteProfile(currentProfile);
                    
                    // Switch back to Main profile
                    LoadProfile("Main");
                    
                    MessageBox.Show($"Profile '{currentProfile}' deleted successfully.", 
                        "Profile Deleted", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting profile: {ex.Message}", 
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void LoadProfile(string profileName)
        {
            try
            {
                // Set current profile
                FileBasedData.SetCurrentProfile(profileName);
                
                // Close all existing windows
                CloseAllMDIWindows();
                
                // Reload the application with the new profile
                InitializeMDI();
                
                // Update window title
                this.Text = $"Program Manager - {profileName}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading profile '{profileName}': {ex.Message}", 
                    "Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion
    }
}
