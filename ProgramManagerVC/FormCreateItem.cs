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
        string id_item;
        private string selectedIconPath;
        private int selectedIconIndex;
        private string parameters;

        public FormCreateItem(string id, string iditem = "0")
        {
            InitializeComponent();
            id_group = id;
            id_item = iditem;
            selectedIconPath = "";
            selectedIconIndex = 0;
            parameters = "";
        }

        private void ButtonBrowser_Click(object sender, EventArgs e)
        {
            if(openFileDialogPath.ShowDialog() == DialogResult.OK)
            {
                textBoxPath.Text = openFileDialogPath.FileName;
                
                // Auto-populate icon path if not already set
                if (string.IsNullOrEmpty(textBoxIconPath.Text))
                {
                    selectedIconIndex = 0; // Default to first icon for new items
                    textBoxIconPath.Text = openFileDialogPath.FileName;
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
            if (id_item != "0")
            {
                DataTable dTable = new DataTable();
                dTable = data.SendQueryWithReturn("SELECT * FROM items WHERE id = " + id_item);
                textBoxName.Text = dTable.Rows[0][1].ToString();
                textBoxPath.Text = dTable.Rows[0][2].ToString();
                // Read parameters column if available
                if (dTable.Columns.Contains("parameters"))
                {
                    try { parameters = dTable.Rows[0]["parameters"].ToString(); } catch { parameters = ""; }
                }
                else
                {
                    parameters = "";
                }

                // Set parameters textbox if present
                if (this.Controls.Find("textBoxParameters", true).Length > 0)
                {
                    var tb = this.Controls.Find("textBoxParameters", true)[0] as TextBox;
                    if (tb != null) tb.Text = parameters;
                }
                
                // Load existing icon info
                string iconInfo = dTable.Rows[0][3].ToString();
                if (iconInfo.Contains("|"))
                {
                    // New format: "path|index"
                    string[] parts = iconInfo.Split('|');
                    selectedIconPath = parts[0];
                    selectedIconIndex = int.Parse(parts[1]);
                    textBoxIconPath.Text = selectedIconPath;
                    LoadIconsFromFile(selectedIconPath);
                }
                else
                {
                    // Old format: just path
                    selectedIconPath = iconInfo;
                    selectedIconIndex = 0;
                    textBoxIconPath.Text = selectedIconPath;
                    LoadIconsFromFile(selectedIconPath);
                }
            }
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
                item.Text = ""; // No text, just icon
                item.ImageIndex = 0;
                item.Tag = 0; // Icon index
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
                for (int i = 0; i < 50; i++) // Try first 50 icons
                {
                    IntPtr hIcon = ExtractIcon(IntPtr.Zero, filePath, i);
                    if (hIcon != IntPtr.Zero && hIcon != (IntPtr)1)
                    {
                        try
                        {
                            Icon icon = Icon.FromHandle(hIcon);
                            imageListIcons.Images.Add(icon.ToBitmap());
                            
                            ListViewItem item = new ListViewItem();
                            item.Text = ""; // No text, just icon
                            item.ImageIndex = imageListIcons.Images.Count - 1;
                            item.Tag = i; // Store the original icon index
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
                        break; // No more icons
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
                            item.Text = ""; // No text, just icon
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
            // Position icons in a horizontal line to prevent vertical scrollbar
            int x = 5; // Starting X position with small margin
            int y = 5; // Fixed Y position for single row with small margin
            int iconSpacing = 38; // 32px icon + 6px spacing
            
            foreach (ListViewItem item in listViewIcons.Items)
            {
                item.Position = new Point(x, y);
                x += iconSpacing; // Move to next position horizontally only
            }
            
            // Force the ListView to recognize the new layout
            listViewIcons.Invalidate();
        }

        private void ListViewIcons_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listViewIcons.SelectedItems.Count > 0)
            {
                selectedIconIndex = (int)listViewIcons.SelectedItems[0].Tag;
                UpdatePreviewIcon();
                CheckTextBoxes(); // Revalidate form
            }
        }

        private void UpdatePreviewIcon()
        {
            try
            {
                if (listViewIcons.SelectedItems.Count > 0)
                {
                    // Get the selected icon from the ImageList
                    int imageIndex = listViewIcons.SelectedItems[0].ImageIndex;
                    if (imageIndex >= 0 && imageIndex < imageListIcons.Images.Count)
                    {
                        pictureBoxPreview.Image = imageListIcons.Images[imageIndex];
                    }
                }
                else
                {
                    // No selection, clear the preview
                    pictureBoxPreview.Image = null;
                }
            }
            catch (Exception)
            {
                // If there's an error, clear the preview
                pictureBoxPreview.Image = null;
            }
        }

        private void SelectIconByIndex()
        {
            if (listViewIcons.Items.Count > 0)
            {
                ListViewItem itemToSelect = null;
                
                // When editing an item, try to find the previously selected icon
                if (id_item != "0")
                {
                    // Find the item with the matching icon index
                    foreach (ListViewItem item in listViewIcons.Items)
                    {
                        if ((int)item.Tag == selectedIconIndex)
                        {
                            itemToSelect = item;
                            break;
                        }
                    }
                }
                
                // If no specific icon found (or creating new item), select first icon
                if (itemToSelect == null)
                {
                    itemToSelect = listViewIcons.Items[0];
                    selectedIconIndex = (int)itemToSelect.Tag;
                }
                
                // Apply the selection
                listViewIcons.SelectedItems.Clear(); // Clear any existing selection
                itemToSelect.Selected = true;
                itemToSelect.Focused = true;
                listViewIcons.EnsureVisible(itemToSelect.Index);
                
                // Update the selected index and preview
                selectedIconIndex = (int)itemToSelect.Tag;
                UpdatePreviewIcon();
            }
        }

        private void TextBoxIconPath_TextChanged(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(textBoxIconPath.Text) && File.Exists(textBoxIconPath.Text))
            {
                LoadIconsFromFile(textBoxIconPath.Text);
            }
            else
            {
                listViewIcons.Items.Clear();
                imageListIcons.Images.Clear();
            }
            CheckTextBoxes();

            // Automatic icon selection for new items
            if (id_item == "0" && listViewIcons.Items.Count > 0)
            {
                // Select the first icon by default for new items
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
            // Validate that the path exists
            if (!System.IO.File.Exists(textBoxPath.Text))
            {
                MessageBox.Show("The specified file does not exist:\n\n" + textBoxPath.Text + 
                               "\n\nPlease check the path and try again.", 
                               "File Not Found", 
                               MessageBoxButtons.OK, 
                               MessageBoxIcon.Warning);
                return; // Stay on the dialog, don't close
            }

            // Prepare icon information for database
            string iconInfo;
            if (!string.IsNullOrEmpty(textBoxIconPath.Text) && File.Exists(textBoxIconPath.Text))
            {
                // Use selected icon
                selectedIconPath = textBoxIconPath.Text;
                iconInfo = selectedIconPath + "|" + selectedIconIndex;
            }
            else
            {
                // Use the executable path as default with index 0
                iconInfo = textBoxPath.Text + "|0";
                selectedIconPath = textBoxPath.Text;
                selectedIconIndex = 0;
            }

            // Read parameters textbox if available
            if (this.Controls.Find("textBoxParameters", true).Length > 0)
            {
                var tb = this.Controls.Find("textBoxParameters", true)[0] as TextBox;
                if (tb != null) parameters = tb.Text;
            }

            try
            {
                // Validation passed - save the item
                if (id_item == "0")
                {
                    data.SendQueryWithoutReturn("INSERT INTO \"items\"(id,name,path,icon,groups,parameters) VALUES (NULL,'" + 
                        textBoxName.Text.Replace("'", "''") + "','" + 
                        textBoxPath.Text.Replace("'", "''") + "','" + 
                        iconInfo.Replace("'", "''") + "','" + id_group + "','" + parameters.Replace("'", "''") + "');");
                }
                else
                {
                    data.SendQueryWithoutReturn("UPDATE items SET name = \"" + textBoxName.Text.Replace("\"", "\"\"") + 
                        "\", path = \"" + textBoxPath.Text.Replace("\"", "\"\"") + 
                        "\", icon = \"" + iconInfo.Replace("\"", "\"\"") + "\", parameters = \"" + parameters.Replace("\"", "\"\"") + "\" WHERE id = " + id_item);
                }
                
                // Only set DialogResult and close when everything is successful
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving item: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CheckTextBoxes()
        {
            if (!string.IsNullOrEmpty(textBoxName.Text) && !string.IsNullOrEmpty(textBoxPath.Text))
            {
                buttonOK.Enabled = true;
            }
            else
            {
                buttonOK.Enabled = false;
            }
        }

        private void TextBoxName_TextChanged(object sender, EventArgs e)
        {
            CheckTextBoxes();
        }

        private void TextBoxPath_TextChanged(object sender, EventArgs e)
        {
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

        // Windows API functions for extracting icons
        [DllImport("shell32.dll")]
        private static extern IntPtr ExtractIcon(IntPtr hInst, string lpszExeFileName, int nIconIndex);

        [DllImport("user32.dll")]
        private static extern bool DestroyIcon(IntPtr hIcon);
    }
}
