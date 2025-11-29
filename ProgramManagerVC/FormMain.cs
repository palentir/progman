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
        public FormMain()
        {
            InitializeComponent();
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
                    SaveWindowSize();
                }
            }
            else if (e.CloseReason == CloseReason.ApplicationExitCall)
            {
                SaveWindowSize();
                e.Cancel = false;
            }
        }

        private void FormMain_Load(object sender, EventArgs e)
        {
            InitializeMDI();
            InitializeTitle();
            LoadWindowSize();
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
            
            foreach (DataRow row in groupsSchema.Rows)
            {
                string columnName = row["name"].ToString();
                if (columnName == "x") hasXColumn = true;
                if (columnName == "y") hasYColumn = true;
                if (columnName == "width") hasWidthColumn = true;
                if (columnName == "height") hasHeightColumn = true;
            }
            
            if (!hasXColumn)
                data.SendQueryWithoutReturn("ALTER TABLE groups ADD COLUMN x INTEGER DEFAULT 0");
            if (!hasYColumn)
                data.SendQueryWithoutReturn("ALTER TABLE groups ADD COLUMN y INTEGER DEFAULT 0");
            if (!hasWidthColumn)
                data.SendQueryWithoutReturn("ALTER TABLE groups ADD COLUMN width INTEGER DEFAULT 300");
            if (!hasHeightColumn)
                data.SendQueryWithoutReturn("ALTER TABLE groups ADD COLUMN height INTEGER DEFAULT 250");
            
            DataTable groups = new DataTable();
            groups = data.SendQueryWithReturn("SELECT * FROM groups");
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
                    
                    switch (groups.Rows[i][2].ToString())
                    {
                        case "0":
                            child.WindowState = FormWindowState.Minimized;
                            break;
                        case "1":
                            child.WindowState = FormWindowState.Normal;
                            break;
                        case "2":
                            child.WindowState = FormWindowState.Maximized;
                            break;
                    }
                    child.Show();
                }
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
            if (((FormChild)this.ActiveMdiChild) == null)
            {
                newItemToolStripMenuItem.Enabled = false;
                deleteToolStripMenuItem.Enabled = false;
                propertiesToolStripMenuItem.Enabled = false;
            }
            else
            {
                newItemToolStripMenuItem.Enabled = true;
                deleteToolStripMenuItem.Enabled = true;
                propertiesToolStripMenuItem.Enabled = true;
            }
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

        private void LoadWindowSize()
        {
            DataTable settings = data.SendQueryWithReturn("SELECT * FROM settings WHERE key = 'window_width' OR key = 'window_height'");
            if (settings.Rows.Count > 0)
            {
                int width = 0;
                int height = 0;
                
                foreach (DataRow row in settings.Rows)
                {
                    if (row[1].ToString() == "window_width")
                    {
                        int.TryParse(row[2].ToString(), out width);
                    }
                    else if (row[1].ToString() == "window_height")
                    {
                        int.TryParse(row[2].ToString(), out height);
                    }
                }
                
                if (width > 0 && height > 0)
                {
                    this.Width = width;
                    this.Height = height;
                }
            }
        }

        private void SaveWindowSize()
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
            }
        }
    }
}
