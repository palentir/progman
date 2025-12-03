using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace ProgramManagerVC
{
    public partial class FormCreateGroup : Form
    {
        string id_group;
        public FormCreateGroup(string id = "0")
        {
            InitializeComponent();
            id_group = id;
        }

        private void ButtonCancel_Click(object sender, EventArgs e)
        {
            this.Hide();
        }

        private void ButtonOK_Click(object sender, EventArgs e)
        {
            if (id_group == "0")
            {
                // Creating new group
                FileBasedData.CreateGroup(textBoxName.Text);
            }
            else
            {
                // Editing existing group
                var groups = FileBasedData.GetAllGroups();
                var group = groups.FirstOrDefault(g => g.Id == id_group);
                
                if (group != null)
                {
                    // If name changed, rename the folder
                    if (group.Name != textBoxName.Text)
                    {
                        FileBasedData.RenameGroup(group.Name, textBoxName.Text);
                    }
                }
            }
            this.Close();
        }

        private void FormCreateGroup_Load(object sender, EventArgs e)
        {
            if(id_group != "0")
            {
                var groups = FileBasedData.GetAllGroups();
                var group = groups.FirstOrDefault(g => g.Id == id_group);
                
                if (group != null)
                {
                    textBoxName.Text = group.Name;
                }
            }
        }

        private void TextBoxName_TextChanged(object sender, EventArgs e)
        {
            buttonOK.Enabled = !string.IsNullOrEmpty(textBoxName.Text) && IsValidFolderName(textBoxName.Text);
        }

        private bool IsValidFolderName(string name)
        {
            // Check for invalid characters in folder names
            char[] invalidChars = Path.GetInvalidFileNameChars();
            return !string.IsNullOrWhiteSpace(name) && name.IndexOfAny(invalidChars) == -1;
        }

        private void textBoxName_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Return && !string.IsNullOrEmpty(textBoxName.Text) && IsValidFolderName(textBoxName.Text))
            {
                buttonOK.PerformClick();
            }
        }
    }
}
