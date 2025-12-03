using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Runtime.InteropServices;

namespace ProgramManagerVC
{
    public partial class FormCreateItem : Form
    {
        string id_group;
        string shortcut_name;
        private ShortcutInfo existingShortcut;
        private string selectedIconPath;
        private int selectedIconIndex;

        public FormCreateItem(string id, string shortcutName = "")
        {
            InitializeComponent();
            id_group = id;
            shortcut_name = shortcutName;
            selectedIconPath = "";
            selectedIconIndex = 0;
        }

        private void ButtonBrowser_Click(object sender, EventArgs e)
        {
            if(openFileDialogPath.ShowDialog() == DialogResult.OK)
            {
                textBoxPath.Text = openFileDialogPath.FileName;
                
                // Auto-populate name if empty
                if (string.IsNullOrEmpty(textBoxName.Text))
                {
                    textBoxName.Text = Path.GetFileNameWithoutExtension(openFileDialogPath.FileName);
                }
                
                // Auto-populate icon from path if icon path is empty
                if (string.IsNullOrEmpty(textBoxIconPath.Text))
                {
                    selectedIconIndex = 0;
                    selectedIconPath = openFileDialogPath.FileName;
                    LoadIconsFromFile(openFileDialogPath.FileName);
                }
            }
        }

        private void ButtonIconBrowser_Click(object sender, EventArgs e)
        {
            if (openFileDialogIcon.ShowDialog() == DialogResult.OK)
            {
                textBoxIconPath.Text = openFileDialogIcon.FileName;
                LoadIconsFromFile(openFileDialogIcon.FileName);
            }
        }

        private void FormCreateItem_Load(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(shortcut_name))
            {
                // Editing existing shortcut
                var groupName = GetGroupNameFromId(id_group);
                var shortcuts = FileBasedData.GetShortcutsInGroup(groupName);
                existingShortcut = shortcuts.FirstOrDefault(s => s.Name == shortcut_name);
                
                if (existingShortcut != null)
                {
                    textBoxName.Text = existingShortcut.Name;
                    textBoxPath.Text = existingShortcut.TargetPath;
                    
                    // Set parameters textbox if available
                    if (this.Controls.Find("textBoxParameters", true).Length > 0)
                    {
                        var tb = this.Controls.Find("textBoxParameters", true)[0] as TextBox;
                        if (tb != null) tb.Text = existingShortcut.Arguments ?? "";
                    }
                    
                    selectedIconPath = existingShortcut.IconLocation;
                    selectedIconIndex = existingShortcut.IconIndex;
                    
                    // Only show icon path if it differs from the executable path
                    if (selectedIconPath == textBoxPath.Text)
                    {
                        textBoxIconPath.Text = "";
                    }
                    else
                    {
                        textBoxIconPath.Text = selectedIconPath;
                    }
                    
                    LoadIconsFromFile(selectedIconPath);
                }
            }
        }

        private string GetGroupNameFromId(string id)
        {
            var groups = FileBasedData.GetAllGroups();
            var group = groups.FirstOrDefault(g => g.Id == id);
            return group?.Name ?? "";
        }

        private void LoadIconsFromFile(string filePath)
        {
            listViewIcons.Items.Clear();
            imageListIcons.Images.Clear();
            
            if (!File.Exists(filePath))
            {
                return;
            }

            try
            {
                string extension = Path.GetExtension(filePath).ToLower();
                
                if (extension == ".ico")
                {
                    LoadIconFromIcoFile(filePath);
                }
                else if (extension == ".exe" || extension == ".dll")
                {
                    LoadIconsFromExecutable(filePath);
                }
                
                // Arrange icons horizontally in a single row
                ArrangeIconsHorizontally();
                
                // Select the current icon after loading
                SelectIconByIndex();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading icons: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void LoadIconFromIcoFile(string filePath)
        {
            try
            {
                Icon icon = new Icon(filePath);
                imageListIcons.Images.Add(icon.ToBitmap());
                
                ListViewItem item = new ListViewItem();
                item.Text = "";
                item.ImageIndex = 0;
                item.Tag = 0;
                listViewIcons.Items.Add(item);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not load icon file: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void LoadIconsFromExecutable(string filePath)
        {
            try
            {
                // Extract icons from executable/dll
                for (int i = 0; i < 50; i++)
                {
                    IntPtr hIcon = ExtractIcon(IntPtr.Zero, filePath, i);
                    if (hIcon != IntPtr.Zero && hIcon != (IntPtr)1)
                    {
                        try
                        {
                            Icon icon = Icon.FromHandle(hIcon);
                            imageListIcons.Images.Add(icon.ToBitmap());
                            
                            ListViewItem item = new ListViewItem();
                            item.Text = "";
                            item.ImageIndex = imageListIcons.Images.Count - 1;
                            item.Tag = i;
                            listViewIcons.Items.Add(item);
                            
                            DestroyIcon(hIcon);
                        }
                        catch
                        {
                            DestroyIcon(hIcon);
                        }
                    }
                    else
                    {
                        break;
                    }
                }
                
                // If no icons found, try to get the default icon
                if (listViewIcons.Items.Count == 0)
                {
                    try
                    {
                        Icon icon = Icon.ExtractAssociatedIcon(filePath);
                        if (icon != null)
                        {
                            imageListIcons.Images.Add(icon.ToBitmap());
                            
                            ListViewItem item = new ListViewItem();
                            item.Text = "";
                            item.ImageIndex = 0;
                            item.Tag = 0;
                            listViewIcons.Items.Add(item);
                        }
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not extract icons from file: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void ArrangeIconsHorizontally()
        {
            int x = 5;
            int y = 5;
            int iconSpacing = 38;
            
            foreach (ListViewItem item in listViewIcons.Items)
            {
                item.Position = new Point(x, y);
                x += iconSpacing;
            }
            
            listViewIcons.Invalidate();
        }

        private void ListViewIcons_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listViewIcons.SelectedItems.Count > 0)
            {
                selectedIconIndex = (int)listViewIcons.SelectedItems[0].Tag;
                UpdatePreviewIcon();
                CheckTextBoxes();
            }
        }

        private void UpdatePreviewIcon()
        {
            try
            {
                if (listViewIcons.SelectedItems.Count > 0)
                {
                    int imageIndex = listViewIcons.SelectedItems[0].ImageIndex;
                    if (imageIndex >= 0 && imageIndex < imageListIcons.Images.Count)
                    {
                        pictureBoxPreview.Image = imageListIcons.Images[imageIndex];
                    }
                }
                else
                {
                    pictureBoxPreview.Image = null;
                }
            }
            catch (Exception)
            {
                pictureBoxPreview.Image = null;
            }
        }

        private void SelectIconByIndex()
        {
            if (listViewIcons.Items.Count > 0)
            {
                ListViewItem itemToSelect = null;
                
                if (existingShortcut != null)
                {
                    foreach (ListViewItem item in listViewIcons.Items)
                    {
                        if ((int)item.Tag == selectedIconIndex)
                        {
                            itemToSelect = item;
                            break;
                        }
                    }
                }
                
                if (itemToSelect == null)
                {
                    itemToSelect = listViewIcons.Items[0];
                    selectedIconIndex = (int)itemToSelect.Tag;
                }
                
                listViewIcons.SelectedItems.Clear();
                itemToSelect.Selected = true;
                itemToSelect.Focused = true;
                listViewIcons.EnsureVisible(itemToSelect.Index);
                
                selectedIconIndex = (int)itemToSelect.Tag;
                UpdatePreviewIcon();
            }
        }

        private void TextBoxIconPath_TextChanged(object sender, EventArgs e)
        {
            string iconPathToLoad = string.IsNullOrEmpty(textBoxIconPath.Text) ? textBoxPath.Text : textBoxIconPath.Text;
            
            if (!string.IsNullOrEmpty(iconPathToLoad) && File.Exists(iconPathToLoad))
            {
                LoadIconsFromFile(iconPathToLoad);
            }
            else
            {
                listViewIcons.Items.Clear();
                imageListIcons.Images.Clear();
            }
            CheckTextBoxes();

            if (existingShortcut == null && listViewIcons.Items.Count > 0)
            {
                selectedIconIndex = 0;
                listViewIcons.Items[0].Selected = true;
                listViewIcons.Items[0].Focused = true;
                listViewIcons.EnsureVisible(0);
            }
        }

        private void textBoxIconPath_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (files != null && files.Any())
            {
                textBoxIconPath.Text = files.First();
            }
        }

        private void textBoxIconPath_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Link;
            else
                e.Effect = DragDropEffects.None;
        }

        private void ButtonOK_Click(object sender, EventArgs e)
        {
            if (!System.IO.File.Exists(textBoxPath.Text))
            {
                MessageBox.Show("The specified file does not exist:\n\n" + textBoxPath.Text + 
                               "\n\nPlease check the path and try again.", 
                               "File Not Found", 
                               MessageBoxButtons.OK, 
                               MessageBoxIcon.Warning);
                return;
            }

            // Get parameters if textbox exists
            string parameters = "";
            if (this.Controls.Find("textBoxParameters", true).Length > 0)
            {
                var tb = this.Controls.Find("textBoxParameters", true)[0] as TextBox;
                if (tb != null) parameters = tb.Text;
            }

            // Determine actual icon path
            string actualIconPath = string.IsNullOrEmpty(textBoxIconPath.Text) ? textBoxPath.Text : textBoxIconPath.Text;

            try
            {
                var groupName = GetGroupNameFromId(id_group);
                
                if (existingShortcut == null)
                {
                    // Creating new shortcut
                    FileBasedData.CreateShortcut(groupName, textBoxName.Text, textBoxPath.Text, parameters, actualIconPath, selectedIconIndex);
                }
                else
                {
                    // Delete old shortcut if name changed
                    if (existingShortcut.Name != textBoxName.Text)
                    {
                        FileBasedData.DeleteShortcut(groupName, existingShortcut.Name + ".lnk");
                    }
                    
                    // Create updated shortcut
                    FileBasedData.CreateShortcut(groupName, textBoxName.Text, textBoxPath.Text, parameters, actualIconPath, selectedIconIndex);
                }
                
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving shortcut: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CheckTextBoxes()
        {
            bool isValid = !string.IsNullOrEmpty(textBoxName.Text) && 
                          !string.IsNullOrEmpty(textBoxPath.Text) &&
                          IsValidFileName(textBoxName.Text);
            
            buttonOK.Enabled = isValid;
        }

        private bool IsValidFileName(string name)
        {
            // Check for invalid characters in file names
            char[] invalidChars = Path.GetInvalidFileNameChars();
            return !string.IsNullOrWhiteSpace(name) && name.IndexOfAny(invalidChars) == -1;
        }

        private void TextBoxName_TextChanged(object sender, EventArgs e)
        {
            CheckTextBoxes();
        }

        private void TextBoxPath_TextChanged(object sender, EventArgs e)
        {
            // When path changes and icon path is empty, reload icons from new path
            if (string.IsNullOrEmpty(textBoxIconPath.Text) && !string.IsNullOrEmpty(textBoxPath.Text) && File.Exists(textBoxPath.Text))
            {
                selectedIconIndex = 0;
                LoadIconsFromFile(textBoxPath.Text);
            }
            CheckTextBoxes();
        }

        private void textBoxPath_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (files != null && files.Any())
                textBoxPath.Text = files.First();
        }

        private void textBoxPath_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Link;
            else
                e.Effect = DragDropEffects.None;
        }

        [DllImport("shell32.dll")]
        private static extern IntPtr ExtractIcon(IntPtr hInst, string lpszExeFileName, int nIconIndex);

        [DllImport("user32.dll")]
        private static extern bool DestroyIcon(IntPtr hIcon);
    }
}
