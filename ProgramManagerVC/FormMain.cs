using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Data.SQLite;
using data = ProgramManagerVC.data;

namespace ProgramManagerVC
{
    public partial class FormMain : Form
    {
        private List<MinimizedIcon> minimizedIcons = new List<MinimizedIcon>();
        private IconHostForm iconHost;

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
                iconHost.SendToBack();
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
                    SaveWindowSizeAndPosition();
                }
            }
            else if (e.CloseReason == CloseReason.ApplicationExitCall)
            {
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
                Text = $"Program Manager - {Environment.MachineName}/{Environment.UserName}";
            }
            else
            {
                Text = "Program Manager";
            }
        }

        private void InitializeMDI()
        {
            data.SendQueryWithoutReturn("CREATE TABLE IF NOT EXISTS \"groups\" (id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT, status INTEGER)");
            data.SendQueryWithoutReturn("CREATE TABLE IF NOT EXISTS \"items\" (id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT, path TEXT, icon TEXT, groups INTEGER)");
            data.SendQueryWithoutReturn("CREATE TABLE IF NOT EXISTS \"settings\" (id INTEGER PRIMARY KEY AUTOINCREMENT, key TEXT, value TEXT)");
            
            DataTable groupsSchema = data.SendQueryWithReturn("PRAGMA table_info(groups)");
            bool hasXColumn = false;
            bool hasYColumn = false;
            bool hasWidthColumn = false;
            bool hasHeightColumn = false;
            bool hasIconIndexColumn = false;
            
            foreach (DataRow row in groupsSchema.Rows)
            {
                string columnName = row["name"].ToString();
                if (columnName == "x") hasXColumn = true;
                if (columnName == "y") hasYColumn = true;
                if (columnName == "width") hasWidthColumn = true;
                if (columnName == "height") hasHeightColumn = true;
                if (columnName == "icon_index") hasIconIndexColumn = true;
            }
            
            if (!hasXColumn)
                data.SendQueryWithoutReturn("ALTER TABLE groups ADD COLUMN x INTEGER DEFAULT 0");
            if (!hasYColumn)
                data.SendQueryWithoutReturn("ALTER TABLE groups ADD COLUMN y INTEGER DEFAULT 0");
            if (!hasWidthColumn)
                data.SendQueryWithoutReturn("ALTER TABLE groups ADD COLUMN width INTEGER DEFAULT 300");
            if (!hasHeightColumn)
                data.SendQueryWithoutReturn("ALTER TABLE groups ADD COLUMN height INTEGER DEFAULT 250");
            if (!hasIconIndexColumn)
                data.SendQueryWithoutReturn("ALTER TABLE groups ADD COLUMN icon_index INTEGER DEFAULT 0");
            
            DataTable groups = new DataTable();
            groups = data.SendQueryWithReturn("SELECT * FROM groups ORDER BY icon_index ASC");
            if (groups.Rows.Count > 0)
            {
                for (int i = 0; i < groups.Rows.Count; i++)
                {
                    Form child = new FormChild();
                    child.Text = groups.Rows[i][1].ToString();
                    child.Tag = groups.Rows[i][0].ToString();
                    child.MdiParent = this;
                    
                    if (groups.Columns.Count >= 7 && groups.Rows[i][3] != DBNull.Value)
                    {
                        int x = Convert.ToInt32(groups.Rows[i][3]);
                        int y = Convert.ToInt32(groups.Rows[i][4]);
                        int width = Convert.ToInt32(groups.Rows[i][5]);
                        int height = Convert.ToInt32(groups.Rows[i][6]);
                        
                        if (width > 0 && height > 0)
                        {
                            child.StartPosition = FormStartPosition.Manual;
                            child.Location = new Point(x, y);
                            child.Size = new Size(width, height);
                        }
                    }
                    
                    int windowStatus = Convert.ToInt32(groups.Rows[i][2]);
                    switch (windowStatus)
                    {
                        case 0:
                            child.WindowState = FormWindowState.Minimized;
                            child.Show();
                            child.Visible = false;
                            AddMinimizedIcon(child);
                            break;
                        case 1:
                            child.WindowState = FormWindowState.Normal;
                            child.Show();
                            break;
                        case 2:
                            child.WindowState = FormWindowState.Maximized;
                            child.Show();
                            break;
                    }
                }
                
                ArrangeMinimizedIcons();
            }
        }

        private void CloseAllMDIWindows()
        {
            foreach (Form frm in this.MdiChildren)
            {
                frm.Visible = false;
                frm.Dispose();
            }
        }

        private void NewItemToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (FormCreateItem createform = new FormCreateItem(this.ActiveMdiChild.Tag.ToString()))
            {
                if (createform.ShowDialog() == DialogResult.OK)
                {
                    ((FormChild)this.ActiveMdiChild).InitializeItems();
                }
            }
        }

        private void DeleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (((FormChild)this.ActiveMdiChild).listViewMain.SelectedItems.Count > 0)
            {
                if (MessageBox.Show("Do you really want to delete the \"" + ((FormChild)this.ActiveMdiChild).listViewMain.SelectedItems[0].Text + "\" item?",
                                   "Confirm",
                                   MessageBoxButtons.YesNo,
                                   MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    data.SendQueryWithoutReturn("DELETE FROM \"items\" WHERE id = " + ((FormChild)this.ActiveMdiChild).listViewMain.SelectedItems[0].Tag);
                    ((FormChild)this.ActiveMdiChild).InitializeItems();
                }
            }
            else
            {
                if (MessageBox.Show("Do you really want to delete \"" + this.ActiveMdiChild.Text + "\" group?",
                               "Confirm",
                               MessageBoxButtons.YesNo,
                               MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    data.SendQueryWithoutReturn("DELETE FROM \"groups\" WHERE id = " + this.ActiveMdiChild.Tag);
                    ((FormChild)this.ActiveMdiChild).Hide();
                }
            }
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
            if (((FormChild)this.ActiveMdiChild).listViewMain.SelectedItems.Count > 0)
            {
                using (FormCreateItem createform = new FormCreateItem(this.ActiveMdiChild.Tag.ToString(), 
                    ((FormChild)this.ActiveMdiChild).listViewMain.SelectedItems[0].Tag.ToString()))
                {
                    if (createform.ShowDialog() == DialogResult.OK)
                    {
                        ((FormChild)this.ActiveMdiChild).InitializeItems();
                    }
                }
            }
            else
            {
                Form createForm = new FormCreateGroup(this.ActiveMdiChild.Tag.ToString());
                if (createForm.ShowDialog() == DialogResult.OK)
                {
                    CloseAllMDIWindows();
                    InitializeMDI();
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

        private void convertFolderToGroupToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DialogResult result = folderBrowserDialogCovnerter.ShowDialog();
            if (result == DialogResult.OK)
            {
                string path = folderBrowserDialogCovnerter.SelectedPath;
                string[] list = Directory.GetFiles(path, "*.lnk");
                string name = path.Replace(Path.GetDirectoryName(path) + Path.DirectorySeparatorChar, "");

                data.SendQueryWithoutReturn("INSERT INTO groups (id,name,status) VALUES (NULL,\"" + name + "\",1)");

                DataTable group = new DataTable();
                group = data.SendQueryWithReturn("SELECT * FROM groups WHERE name = \"" + name + "\";");

                foreach (string Link in list)
                {
                    string itemName = Path.GetFileNameWithoutExtension(Link);
                    data.SendQueryWithoutReturn("INSERT INTO \"items\"(id,name,path,icon,groups) VALUES (NULL,'" + itemName + "','" + Link + "','" + Link + "','" + group.Rows[0][0].ToString() + "');");
                }

                CloseAllMDIWindows();
                InitializeMDI();
            }
        }

        private void LoadWindowSizeAndPosition()
        {
            DataTable settings = data.SendQueryWithReturn("SELECT * FROM settings WHERE key IN ('window_width', 'window_height', 'window_x', 'window_y')");
            if (settings.Rows.Count > 0)
            {
                int width = 0;
                int height = 0;
                int x = -1;
                int y = -1;
                
                foreach (DataRow row in settings.Rows)
                {
                    string key = row[1].ToString();
                    if (key == "window_width")
                    {
                        int.TryParse(row[2].ToString(), out width);
                    }
                    else if (key == "window_height")
                    {
                        int.TryParse(row[2].ToString(), out height);
                    }
                    else if (key == "window_x")
                    {
                        int.TryParse(row[2].ToString(), out x);
                    }
                    else if (key == "window_y")
                    {
                        int.TryParse(row[2].ToString(), out y);
                    }
                }
                
                if (width > 0 && height > 0)
                {
                    this.Width = width;
                    this.Height = height;
                }
                
                if (x >= 0 && y >= 0)
                {
                    if (IsLocationOnScreen(x, y, this.Width, this.Height))
                    {
                        this.StartPosition = FormStartPosition.Manual;
                        this.Location = new Point(x, y);
                    }
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
                DataTable existingWidth = data.SendQueryWithReturn("SELECT * FROM settings WHERE key = 'window_width'");
                if (existingWidth.Rows.Count > 0)
                {
                    data.SendQueryWithoutReturn("UPDATE settings SET value = '" + this.Width + "' WHERE key = 'window_width'");
                }
                else
                {
                    data.SendQueryWithoutReturn("INSERT INTO settings (id, key, value) VALUES (NULL, 'window_width', '" + this.Width + "')");
                }

                DataTable existingHeight = data.SendQueryWithReturn("SELECT * FROM settings WHERE key = 'window_height'");
                if (existingHeight.Rows.Count > 0)
                {
                    data.SendQueryWithoutReturn("UPDATE settings SET value = '" + this.Height + "' WHERE key = 'window_height'");
                }
                else
                {
                    data.SendQueryWithoutReturn("INSERT INTO settings (id, key, value) VALUES (NULL, 'window_height', '" + this.Height + "')");
                }

                DataTable existingX = data.SendQueryWithReturn("SELECT * FROM settings WHERE key = 'window_x'");
                if (existingX.Rows.Count > 0)
                {
                    data.SendQueryWithoutReturn("UPDATE settings SET value = '" + this.Location.X + "' WHERE key = 'window_x'");
                }
                else
                {
                    data.SendQueryWithoutReturn("INSERT INTO settings (id, key, value) VALUES (NULL, 'window_x', '" + this.Location.X + "')");
                }

                DataTable existingY = data.SendQueryWithReturn("SELECT * FROM settings WHERE key = 'window_y'");
                if (existingY.Rows.Count > 0)
                {
                    data.SendQueryWithoutReturn("UPDATE settings SET value = '" + this.Location.Y + "' WHERE key = 'window_y'");
                }
                else
                {
                    data.SendQueryWithoutReturn("INSERT INTO settings (id, key, value) VALUES (NULL, 'window_y', '" + this.Location.Y + "')");
                }
            }
        }

        private void FormMain_MdiChildActivate(object sender, EventArgs e)
        {
            EnsureIconHostLayout();
            ArrangeMinimizedIcons();
        }

        private void FormMain_Resize(object sender, EventArgs e)
        {
            EnsureIconHostLayout();
            ArrangeMinimizedIcons();
        }

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
            int iconHeight = minimizedIcons.Count > 0 ? minimizedIcons[0].Height : 64; // use actual control height

            int availableWidth = Math.Max(0, host.ClientSize.Width - (spacing * 2));
            int iconsPerRow = Math.Max(1, availableWidth / (iconWidth + spacing));

            // Calculate rows and total height needed
            int totalIcons = minimizedIcons.Count;
            int rows = Math.Max(1, (int)Math.Ceiling(totalIcons / (double)iconsPerRow));
            int totalHeightNeeded = rows * (iconHeight + spacing) + spacing; // including top/bottom spacing

            // Determine starting Y so that all rows are fully visible (no overflow below)
            int startX = spacing;
            int startY = host.ClientSize.Height - totalHeightNeeded;
            if (startY < spacing)
            {
                startY = spacing; // clamp to keep spacing at top
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
                        // move to next row up
                        rowCount++;
                        placedInRow = 0;
                        currentX = startX;
                        currentY += iconHeight + spacing; // layout from top to bottom within startY block
                        if (currentY + iconHeight > host.ClientSize.Height - spacing)
                        {
                            // prevent partial clipping bottom
                            currentY = Math.Max(spacing, host.ClientSize.Height - iconHeight - spacing);
                        }
                    }
                    else
                    {
                        currentX += iconWidth + spacing;
                    }
                }
            }

            iconHost?.SendToBack();
        }

        public void AddMinimizedIcon(Form form)
        {
            MinimizedIcon existingIcon = minimizedIcons.FirstOrDefault(i => i.AssociatedForm == form);
            if (existingIcon == null)
            {
                int iconIndex = GetIconIndexForForm(form);

                MinimizedIcon icon = new MinimizedIcon(form);
                icon.Font = new Font("MS Sans Serif", 8F);

                if (iconIndex >= 0 && iconIndex < minimizedIcons.Count)
                {
                    minimizedIcons.Insert(iconIndex, icon);
                }
                else
                {
                    minimizedIcons.Add(icon);
                }

                var host = GetIconHost();
                host.Controls.Add(icon);
                ArrangeMinimizedIcons();
            }
        }

        public void RemoveMinimizedIcon(Form form)
        {
            MinimizedIcon icon = minimizedIcons.FirstOrDefault(i => i.AssociatedForm == form);
            if (icon != null)
            {
                int index = minimizedIcons.IndexOf(icon);
                SaveIconIndex(form, index);

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

                SaveIconIndex(icon1.AssociatedForm, index2);
                SaveIconIndex(icon2.AssociatedForm, index1);

                ArrangeMinimizedIcons();
            }
        }

        private int GetIconIndexForForm(Form form)
        {
            if (form.Tag == null) return -1;
            
            DataTable result = data.SendQueryWithReturn("SELECT icon_index FROM groups WHERE id = " + form.Tag);
            if (result.Rows.Count > 0 && result.Rows[0][0] != DBNull.Value)
            {
                return Convert.ToInt32(result.Rows[0][0]);
            }
            
            return minimizedIcons.Count;
        }

        private void SaveIconIndex(Form form, int index)
        {
            if (form.Tag != null)
            {
                data.SendQueryWithoutReturn("UPDATE groups SET icon_index = " + index + " WHERE id = " + form.Tag);
            }
        }
    }
}
