using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace ProgramManagerVC
{
    public class MinimizedIcon : UserControl
    {
        private PictureBox iconBox;
        private Panel textPanel;
        private Label titleLabel;
        private Form associatedForm;
        private FormMain mainForm; // Store reference to FormMain
        private Icon groupsIcon;
        private Point mouseDownPoint;
        private bool isDragging = false;
        private bool selected = false;

        public bool Selected
        {
            get { return selected; }
            set
            {
                if (selected != value)
                {
                    selected = value;
                    ApplySelectionVisual();
                }
            }
        }
        
        public MinimizedIcon(Form form, FormMain main)
        {
            associatedForm = form;
            mainForm = main; // Store the reference
            InitializeControl();
        }
        
        private void InitializeControl()
        {
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserMouse | ControlStyles.ResizeRedraw, true);
            
            this.Size = new Size(64, 64);
            this.BackColor = Color.Transparent;
            this.Cursor = Cursors.Hand;
            this.AllowDrop = false;
            
            groupsIcon = GetGroupsIconFromForm();
            
            iconBox = new PictureBox();
            iconBox.Size = new Size(32, 32);
            iconBox.Location = new Point((this.Width - 32) / 2, 0);
            iconBox.SizeMode = PictureBoxSizeMode.StretchImage;
            iconBox.BackColor = Color.Transparent;
            iconBox.Cursor = Cursors.Hand;
            
            if (groupsIcon != null)
            {
                iconBox.Image = groupsIcon.ToBitmap();
            }
            else if (associatedForm.Icon != null)
            {
                iconBox.Image = associatedForm.Icon.ToBitmap();
            }
            
            iconBox.MouseDown += Control_MouseDown;
            iconBox.MouseMove += Control_MouseMove;
            iconBox.MouseUp += Control_MouseUp;
            iconBox.DoubleClick += Control_DoubleClick;
            this.Controls.Add(iconBox);
            
            // Create a panel to hold the text with solid background
            textPanel = new Panel();
            textPanel.Location = new Point(0, 34);
            textPanel.Size = new Size(64, 30);
            textPanel.BackColor = SystemColors.Window;
            textPanel.Cursor = Cursors.Hand;
            textPanel.MouseDown += Control_MouseDown;
            textPanel.MouseMove += Control_MouseMove;
            textPanel.MouseUp += Control_MouseUp;
            textPanel.DoubleClick += Control_DoubleClick;
            
            // Create label inside the panel
            titleLabel = new Label();
            titleLabel.Dock = DockStyle.Fill;
            titleLabel.TextAlign = ContentAlignment.TopCenter;
            titleLabel.Text = associatedForm.Text;
            titleLabel.Font = new Font("MS Sans Serif", 8F);
            titleLabel.BackColor = Color.Transparent; // transparent within panel
            titleLabel.ForeColor = SystemColors.ControlText;
            titleLabel.Cursor = Cursors.Hand;
            titleLabel.MouseDown += Control_MouseDown;
            titleLabel.MouseMove += Control_MouseMove;
            titleLabel.MouseUp += Control_MouseUp;
            titleLabel.DoubleClick += Control_DoubleClick;
            
            textPanel.Controls.Add(titleLabel);
            this.Controls.Add(textPanel);
            
            this.MouseDown += Control_MouseDown;
            this.MouseMove += Control_MouseMove;
            this.MouseUp += Control_MouseUp;
            this.DoubleClick += Control_DoubleClick;
        }

        private void ApplySelectionVisual()
        {
            if (textPanel == null) return;
            
            if (selected)
            {
                // Blue background with white text
                textPanel.BackColor = SystemColors.Highlight;
                if (titleLabel != null)
                {
                    titleLabel.ForeColor = SystemColors.HighlightText;
                }
            }
            else
            {
                // Return to normal
                textPanel.BackColor = SystemColors.Window;
                if (titleLabel != null)
                {
                    titleLabel.ForeColor = SystemColors.ControlText;
                }
            }
            
            // Force immediate redraw
            textPanel.Invalidate();
            textPanel.Update();
            if (titleLabel != null)
            {
                titleLabel.Invalidate();
                titleLabel.Update();
            }
        }
        
        private Icon GetGroupsIconFromForm()
        {
            try
            {
                System.ComponentModel.ComponentResourceManager resources = 
                    new System.ComponentModel.ComponentResourceManager(typeof(FormChild));
                Icon groupsIco = (Icon)resources.GetObject("Groups");
                return groupsIco;
            }
            catch { return null; }
        }
        
        private void Control_DoubleClick(object sender, EventArgs e)
        {
            isDragging = false;
            RestoreWindow();
        }
        
        private void Control_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                // Use stored reference instead of FindForm
                if (mainForm != null)
                {
                    mainForm.SetSelectedIcon(this);
                }
                mouseDownPoint = e.Location;
                isDragging = false;
            }
            else if (e.Button == MouseButtons.Right)
            {
                ShowContextMenu(e.Location);
            }
        }

        private void Control_MouseMove(object sender, MouseEventArgs e)
        {
            // drag-drop disabled
        }

        private void Control_MouseUp(object sender, MouseEventArgs e)
        {
            isDragging = false;
        }
        
        private void RestoreWindow()
        {
            if (associatedForm != null && !associatedForm.IsDisposed)
            {
                FormChild child = associatedForm as FormChild;
                if (child != null)
                {
                    if (child.Tag != null)
                    {
                        DataTable dt = ProgramManagerVC.data.SendQueryWithReturn("SELECT x,y,width,height FROM groups WHERE id = " + child.Tag);
                        if (dt.Rows.Count > 0)
                        {
                            int x = SafeInt(dt.Rows[0][0]);
                            int y = SafeInt(dt.Rows[0][1]);
                            int w = SafeInt(dt.Rows[0][2]);
                            int h = SafeInt(dt.Rows[0][3]);
                            if (w > 50 && h > 50)
                            {
                                child.StartPosition = FormStartPosition.Manual;
                                child.Location = new Point(x, y);
                                child.Size = new Size(w, h);
                            }
                        }
                    }

                    if (!child.Visible) child.Visible = true;
                    child.WindowState = FormWindowState.Normal;
                    child.BringToFront();
                    child.Activate();

                    if (child.Tag != null)
                    {
                        ProgramManagerVC.data.SendQueryWithoutReturn("UPDATE groups SET status=1 WHERE id=" + child.Tag);
                    }

                    // Use stored reference instead of FindForm
                    if (mainForm != null)
                    {
                        mainForm.RemoveMinimizedIcon(child);
                    }
                }
            }
        }

        private int SafeInt(object o)
        {
            int v; if (o == null || o == DBNull.Value) return 0; if (!int.TryParse(o.ToString(), out v)) return 0; return v;
        }

        private void ShowContextMenu(Point location)
        {
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.RenderMode = ToolStripRenderMode.System;
            
            ToolStripMenuItem restoreItem = new ToolStripMenuItem("&Restore");
            restoreItem.Click += (s, ev) => RestoreWindow();
            restoreItem.Font = new Font(restoreItem.Font, FontStyle.Bold);
            menu.Items.Add(restoreItem);
            
            menu.Items.Add(new ToolStripSeparator());
            
            ToolStripMenuItem moveItem = new ToolStripMenuItem("&Move");
            moveItem.Enabled = false;
            menu.Items.Add(moveItem);
            
            menu.Show(this, location);
        }
        
        public Form AssociatedForm => associatedForm;
        
        public void UpdateText(string text)
        {
            if (titleLabel != null)
            {
                titleLabel.Text = text;
            }
        }
    }
}
