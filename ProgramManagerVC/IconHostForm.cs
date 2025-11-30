using System.Windows.Forms;
using System.Drawing;

namespace ProgramManagerVC
{
    internal class IconHostForm : Form
    {
        public IconHostForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            ControlBox = false;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            BackColor = SystemColors.Window;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            // Clicking on empty background clears selection
            var main = this.MdiParent as FormMain;
            if (main != null)
            {
                main.SetSelectedIcon(null);
            }
        }
    }
}
