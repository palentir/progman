using System;
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
        
        public MinimizedIcon(Form form)
        {
            associatedForm = form;
            InitializeControl();
        }
        
        private void InitializeControl()
        {
            this.Size = new Size(80, 80);
            this.BackColor = SystemColors.Control;
            this.Cursor = Cursors.Hand;
            
            groupsIcon = GetGroupsIconFromForm();
            
            iconBox = new PictureBox();
            iconBox.Size = new Size(48, 48);
            iconBox.Location = new Point(16, 0);
            iconBox.SizeMode = PictureBoxSizeMode.StretchImage;
            iconBox.BackColor = SystemColors.Control;
            
            if (groupsIcon != null)
            {
                iconBox.Image = groupsIcon.ToBitmap();
            }
            else
            {
                iconBox.Image = associatedForm.Icon.ToBitmap();
            }
            
            iconBox.MouseDown += Control_MouseDown;
            this.Controls.Add(iconBox);
            
            titleLabel = new Label();
            titleLabel.Location = new Point(0, 52);
            titleLabel.Size = new Size(80, 28);
            titleLabel.TextAlign = ContentAlignment.TopCenter;
            titleLabel.Text = associatedForm.Text;
            titleLabel.Font = new Font("MS Sans Serif", 7F);
            titleLabel.BackColor = SystemColors.Control;
            titleLabel.MouseDown += Control_MouseDown;
            this.Controls.Add(titleLabel);
            
            this.MouseDown += Control_MouseDown;
        }
        
        private Icon GetGroupsIconFromForm()
        {
            try
            {
                FormChild childForm = associatedForm as FormChild;
                if (childForm != null)
                {
                    System.ComponentModel.ComponentResourceManager resources = 
                        new System.ComponentModel.ComponentResourceManager(typeof(FormChild));
                    Icon groupsIco = ((Icon)(resources.GetObject("Groups")));
                    if (groupsIco != null)
                    {
                        return groupsIco;
                    }
                }
            }
            catch
            {
            }
            return null;
        }
        
        private void Control_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (e.Clicks == 2)
                {
                    RestoreWindow();
                }
            }
            else if (e.Button == MouseButtons.Right)
            {
                ShowContextMenu(e.Location);
            }
        }
        
        private void RestoreWindow()
        {
            if (associatedForm != null && !associatedForm.IsDisposed)
            {
                associatedForm.WindowState = FormWindowState.Normal;
                associatedForm.BringToFront();
            }
        }

        private void ShowContextMenu(Point location)
        {
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.RenderMode = ToolStripRenderMode.System;
            
            ToolStripMenuItem restoreItem = new ToolStripMenuItem("Restore");
            restoreItem.Click += (s, ev) => RestoreWindow();
            menu.Items.Add(restoreItem);
            
            menu.Show(this, location);
        }
        
        public Form AssociatedForm
        {
            get { return associatedForm; }
        }
        
        public void UpdateText(string text)
        {
            if (titleLabel != null)
            {
                titleLabel.Text = text;
            }
        }
        
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (iconBox != null)
                {
                    iconBox.Dispose();
                }
                if (titleLabel != null)
                {
                    titleLabel.Dispose();
                }
            }
            base.Dispose(disposing);
        }
    }
}
