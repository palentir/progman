using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace ProgramManagerVC
{
    public class MinimizedIcon : UserControl
    {
        private PictureBox iconBox;
        private Label titleLabel;
        private Form associatedForm;
        private Icon groupsIcon;
        private Point mouseDownPoint;
        private bool isDragging = false;
        
        public MinimizedIcon(Form form)
        {
            associatedForm = form;
            InitializeControl();
        }
        
        private void InitializeControl()
        {
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserMouse, true);
            
            this.Size = new Size(64, 64);
            this.BackColor = Color.Transparent;
            this.Cursor = Cursors.Hand;
            this.AllowDrop = true;
            
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
            iconBox.DoubleClick += Control_DoubleClick; // ensure double-click works when clicking icon
            this.Controls.Add(iconBox);
            
            titleLabel = new Label();
            titleLabel.Location = new Point(0, 34);
            titleLabel.Size = new Size(64, 30);
            titleLabel.TextAlign = ContentAlignment.TopCenter;
            titleLabel.Text = associatedForm.Text;
            titleLabel.Font = new Font("MS Sans Serif", 8F);
            titleLabel.BackColor = Color.Transparent;
            titleLabel.Cursor = Cursors.Hand;
            titleLabel.MouseDown += Control_MouseDown;
            titleLabel.MouseMove += Control_MouseMove;
            titleLabel.MouseUp += Control_MouseUp;
            titleLabel.DoubleClick += Control_DoubleClick; // ensure double-click works when clicking text
            this.Controls.Add(titleLabel);
            
            this.MouseDown += Control_MouseDown;
            this.MouseMove += Control_MouseMove;
            this.MouseUp += Control_MouseUp;
            this.DoubleClick += Control_DoubleClick; // also handle double-click on background
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
            if (e.Button == MouseButtons.Left && !isDragging)
            {
                int dx = Math.Abs(e.X - mouseDownPoint.X);
                int dy = Math.Abs(e.Y - mouseDownPoint.Y);
                if (dx > 5 || dy > 5)
                {
                    isDragging = true;
                    DoDragDrop(this, DragDropEffects.Move);
                }
            }
        }

        private void Control_MouseUp(object sender, MouseEventArgs e)
        {
            isDragging = false;
        }

        protected override void OnDragEnter(DragEventArgs drgevent)
        {
            base.OnDragEnter(drgevent);
            if (drgevent.Data.GetDataPresent(typeof(MinimizedIcon)))
            {
                drgevent.Effect = DragDropEffects.Move;
            }
            else
            {
                drgevent.Effect = DragDropEffects.None;
            }
        }

        protected override void OnDragDrop(DragEventArgs drgevent)
        {
            base.OnDragDrop(drgevent);
            if (drgevent.Data.GetDataPresent(typeof(MinimizedIcon)))
            {
                MinimizedIcon draggedIcon = (MinimizedIcon)drgevent.Data.GetData(typeof(MinimizedIcon));
                if (draggedIcon != null && draggedIcon != this)
                {
                    FormMain mainForm = this.FindForm() as FormMain; // Parent is MDI client; use FindForm
                    if (mainForm != null)
                    {
                        mainForm.SwapIconPositions(draggedIcon, this);
                    }
                }
            }
        }
        
        private void RestoreWindow()
        {
            if (associatedForm != null && !associatedForm.IsDisposed)
            {
                FormMain mainForm = this.FindForm() as FormMain;
                FormChild child = associatedForm as FormChild;
                if (child != null)
                {
                    // Load saved bounds from database (last normal state) before showing
                    if (child.Tag != null)
                    {
                        DataTable dt = ProgramManagerVC.data.SendQueryWithReturn("SELECT x,y,width,height FROM groups WHERE id = " + child.Tag);
                        if (dt.Rows.Count > 0)
                        {
                            int x = SafeInt(dt.Rows[0][0]);
                            int y = SafeInt(dt.Rows[0][1]);
                            int w = SafeInt(dt.Rows[0][2]);
                            int h = SafeInt(dt.Rows[0][3]);
                            if (w > 50 && h > 50) // basic sanity
                            {
                                child.StartPosition = FormStartPosition.Manual;
                                child.Location = new Point(x, y);
                                child.Size = new Size(w, h);
                            }
                        }
                    }

                    // Show and restore
                    if (!child.Visible) child.Visible = true;
                    child.WindowState = FormWindowState.Normal;
                    child.BringToFront();
                    child.Activate();

                    // Update status in DB to normal (1)
                    if (child.Tag != null)
                    {
                        ProgramManagerVC.data.SendQueryWithoutReturn("UPDATE groups SET status=1 WHERE id=" + child.Tag);
                    }

                    // Remove minimized icon explicitly
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
            
            // Show relative to parent control so the position matches click area
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
