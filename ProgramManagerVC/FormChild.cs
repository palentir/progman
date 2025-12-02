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
            Process.Start(listViewMain.SelectedItems[0].ToolTipText.ToString());
        }

        public void InitializeItems()
        {
            listViewMain.Items.Clear();
            imageListIcons.Images.Clear();
            DataTable items = new DataTable();
            items = data.SendQueryWithReturn("SELECT * FROM items WHERE groups = " + this.Tag);
            if (items.Rows.Count > 0)
            {
                for (int i = 0; i < items.Rows.Count; i++)
                {
                    try
                    {
                        imageListIcons.Images.Add(Icon.ExtractAssociatedIcon(items.Rows[i][3].ToString()).ToBitmap());
                        ListViewItem item = new ListViewItem();
                        item.Text = items.Rows[i][1].ToString();
                        item.ImageIndex = i;
                        item.ToolTipText = items.Rows[i][2].ToString();
                        item.Tag = items.Rows[i][0].ToString();
                        listViewMain.Items.Add(item);
                    }
                    catch (Exception)
                    {
                        MessageBox.Show("File \"" + items.Rows[i][3].ToString() + "\" cannot be found. Icon will be deleted.", 
                            "Error", 
                            MessageBoxButtons.OK, 
                            MessageBoxIcon.Error);

                    }
                }
            }
        }

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
            int status = 1;
            if (this.WindowState == FormWindowState.Minimized)
            {
                status = 0;
            }
            else if (this.WindowState == FormWindowState.Normal)
            {
                status = 1;
            }
            else if (this.WindowState == FormWindowState.Maximized)
            {
                status = 2;
            }
            
            if (this.WindowState == FormWindowState.Normal)
            {
                data.SendQueryWithoutReturn("UPDATE groups SET status=" + status + 
                    ", x=" + this.Location.X + 
                    ", y=" + this.Location.Y + 
                    ", width=" + this.Width + 
                    ", height=" + this.Height + 
                    " WHERE id=" + this.Tag);
            }
            else
            {
                data.SendQueryWithoutReturn("UPDATE groups SET status=" + status + " WHERE id=" + this.Tag);
            }
        }

        private void listViewMain_MouseDown(object sender, MouseEventArgs e) 
        {
            if (e.Button == MouseButtons.Right) 
            {
                if (listViewMain.FocusedItem == null)
                {
                    ListMenu.Show(Cursor.Position);
                }
                else if (listViewMain.FocusedItem.Bounds.Contains(e.Location)) 
                {
                    FileMenu.Show(Cursor.Position);
                } 
                else 
                {
                    ListMenu.Show(Cursor.Position);
                }
            }
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e) 
        {
            Process.Start(listViewMain.SelectedItems[0].ToolTipText.ToString());
        }

        private void editToolStripMenuItem_Click(object sender, EventArgs e) 
        {
            Process.Start(new ProcessStartInfo ("explorer.exe", "/select, " + listViewMain.SelectedItems[0].ToolTipText.ToString()));
        }

        private void runAsAdministratorToolStripMenuItem_Click(object sender, EventArgs e) 
        {
            if (System.Environment.OSVersion.Version.Major >= 6) 
            {
                try 
                {
                    Process proc = new Process();
                    proc.StartInfo.FileName = listViewMain.SelectedItems[0].ToolTipText.ToString();
                    proc.StartInfo.UseShellExecute = true;
                    proc.StartInfo.Verb = "runas";
                    proc.Start();
                }
                catch 
                { }
            }
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e) 
        {
            if (MessageBox.Show("Do you really want to delete the \"" + listViewMain.SelectedItems[0].Text + "\" item?",
                                   "Confirm",
                                   MessageBoxButtons.YesNo,
                                   MessageBoxIcon.Question) == DialogResult.Yes) 
            {
                data.SendQueryWithoutReturn("DELETE FROM \"items\" WHERE id = " + listViewMain.SelectedItems[0].Tag);
                this.InitializeItems();
            }
        }

        private void newItemToolStripMenuItem_Click(object sender, EventArgs e) 
        {
            using (FormCreateItem createform = new FormCreateItem(this.Tag.ToString())) 
            {
                if (createform.ShowDialog() == DialogResult.OK) 
                {
                    this.InitializeItems();
                }
            }
        }

        private void propertiesToolStripMenuItem_Click(object sender, EventArgs e)
        {
                using (FormCreateItem createform = new FormCreateItem(this.Tag.ToString(),
                    this.listViewMain.SelectedItems[0].Tag.ToString()))
                {
                    if (createform.ShowDialog() == DialogResult.OK)
                    {
                        InitializeItems();
                    }
                }
           
        }

        private void propertiesToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            Form createForm = new FormCreateGroup(this.Tag.ToString());
            if (createForm.ShowDialog() == DialogResult.OK)
            {
                DataTable db = new DataTable();
                db = data.SendQueryWithReturn("SELECT * FROM groups WHERE id = '" + this.Tag.ToString() + "';");
                this.Text = db.Rows[0][1].ToString();
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
                data.SendQueryWithoutReturn("DELETE FROM \"groups\" WHERE id = " + this.Tag);
                this.Hide();
                this.DestroyHandle();
            }
        }
    }
}
