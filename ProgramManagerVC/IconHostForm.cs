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
            BackColor = SystemColors.Control;
            // Prevent from stealing activation unnecessarily
            // but still allow child controls to receive input
        }
    }
}
