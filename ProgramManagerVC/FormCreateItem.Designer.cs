namespace ProgramManagerVC
{
    partial class FormCreateItem
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.button1 = new System.Windows.Forms.Button();
            this.buttonOK = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.textBoxName = new System.Windows.Forms.TextBox();
            this.textBoxPath = new System.Windows.Forms.TextBox();
            this.buttonBrowser = new System.Windows.Forms.Button();
            this.openFileDialogPath = new System.Windows.Forms.OpenFileDialog();
            this.label3 = new System.Windows.Forms.Label();
            this.textBoxIconPath = new System.Windows.Forms.TextBox();
            this.buttonIconBrowser = new System.Windows.Forms.Button();
            this.openFileDialogIcon = new System.Windows.Forms.OpenFileDialog();
            this.listViewIcons = new System.Windows.Forms.ListView();
            this.imageListIcons = new System.Windows.Forms.ImageList(this.components);
            this.pictureBoxPreview = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxPreview)).BeginInit();
            this.SuspendLayout();
            // 
            // button1
            // 
            this.button1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.button1.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.button1.Location = new System.Drawing.Point(375, 145);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 0;
            this.button1.Text = "Cancel";
            this.button1.UseVisualStyleBackColor = true;
            // 
            // buttonOK
            // 
            this.buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonOK.Enabled = false;
            this.buttonOK.Location = new System.Drawing.Point(375, 116);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.Size = new System.Drawing.Size(75, 23);
            this.buttonOK.TabIndex = 1;
            this.buttonOK.Text = "OK";
            this.buttonOK.UseVisualStyleBackColor = true;
            this.buttonOK.Click += new System.EventHandler(this.ButtonOK_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(39, 17);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(38, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Name:";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(2, 43);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(75, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Shortcut Path:";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // textBoxName
            // 
            this.textBoxName.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.textBoxName.Location = new System.Drawing.Point(83, 14);
            this.textBoxName.Name = "textBoxName";
            this.textBoxName.Size = new System.Drawing.Size(283, 20);
            this.textBoxName.TabIndex = 4;
            this.textBoxName.TextChanged += new System.EventHandler(this.TextBoxName_TextChanged);
            // 
            // textBoxPath
            // 
            this.textBoxPath.AllowDrop = true;
            this.textBoxPath.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.textBoxPath.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.FileSystem;
            this.textBoxPath.Location = new System.Drawing.Point(83, 40);
            this.textBoxPath.Name = "textBoxPath";
            this.textBoxPath.Size = new System.Drawing.Size(244, 20);
            this.textBoxPath.TabIndex = 5;
            this.textBoxPath.TextChanged += new System.EventHandler(this.TextBoxPath_TextChanged);
            this.textBoxPath.DragDrop += new System.Windows.Forms.DragEventHandler(this.textBoxPath_DragDrop);
            this.textBoxPath.DragOver += new System.Windows.Forms.DragEventHandler(this.textBoxPath_DragOver);
            // 
            // buttonBrowser
            // 
            this.buttonBrowser.Location = new System.Drawing.Point(333, 40);
            this.buttonBrowser.Name = "buttonBrowser";
            this.buttonBrowser.Size = new System.Drawing.Size(33, 20);
            this.buttonBrowser.TabIndex = 6;
            this.buttonBrowser.Text = "...";
            this.buttonBrowser.UseVisualStyleBackColor = true;
            this.buttonBrowser.Click += new System.EventHandler(this.ButtonBrowser_Click);
            // 
            // openFileDialogPath
            // 
            this.openFileDialogPath.Filter = "Executable (*.exe)|*.exe|BAT FIles|*.bat|All files|*.*";
            this.openFileDialogPath.Title = "Browse...";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(21, 72);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(56, 13);
            this.label3.TabIndex = 7;
            this.label3.Text = "Icon Path:";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // textBoxIconPath
            // 
            this.textBoxIconPath.AllowDrop = true;
            this.textBoxIconPath.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.textBoxIconPath.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.FileSystem;
            this.textBoxIconPath.Location = new System.Drawing.Point(83, 69);
            this.textBoxIconPath.Name = "textBoxIconPath";
            this.textBoxIconPath.Size = new System.Drawing.Size(244, 20);
            this.textBoxIconPath.TabIndex = 8;
            this.textBoxIconPath.TextChanged += new System.EventHandler(this.TextBoxIconPath_TextChanged);
            this.textBoxIconPath.DragDrop += new System.Windows.Forms.DragEventHandler(this.textBoxIconPath_DragDrop);
            this.textBoxIconPath.DragOver += new System.Windows.Forms.DragEventHandler(this.textBoxIconPath_DragOver);
            // 
            // buttonIconBrowser
            // 
            this.buttonIconBrowser.Location = new System.Drawing.Point(332, 69);
            this.buttonIconBrowser.Name = "buttonIconBrowser";
            this.buttonIconBrowser.Size = new System.Drawing.Size(34, 20);
            this.buttonIconBrowser.TabIndex = 9;
            this.buttonIconBrowser.Text = "...";
            this.buttonIconBrowser.UseVisualStyleBackColor = true;
            this.buttonIconBrowser.Click += new System.EventHandler(this.ButtonIconBrowser_Click);
            // 
            // openFileDialogIcon
            // 
            this.openFileDialogIcon.Filter = "All Icon Sources (*.ico;*.exe;*.dll)|*.ico;*.exe;*.dll|Icon Files (*.ico)|*.ico|E" +
    "xecutable Files (*.exe)|*.exe|Library Files (*.dll)|*.dll|All Files (*.*)|*.*";
            this.openFileDialogIcon.Title = "Select Icon File";
            // 
            // listViewIcons
            // 
            this.listViewIcons.AutoArrange = false;
            this.listViewIcons.HideSelection = false;
            this.listViewIcons.LargeImageList = this.imageListIcons;
            this.listViewIcons.Location = new System.Drawing.Point(83, 95);
            this.listViewIcons.MultiSelect = false;
            this.listViewIcons.Name = "listViewIcons";
            this.listViewIcons.Size = new System.Drawing.Size(284, 70);
            this.listViewIcons.TabIndex = 10;
            this.listViewIcons.UseCompatibleStateImageBehavior = false;
            this.listViewIcons.SelectedIndexChanged += new System.EventHandler(this.ListViewIcons_SelectedIndexChanged);
            // 
            // imageListIcons
            // 
            this.imageListIcons.ColorDepth = System.Windows.Forms.ColorDepth.Depth32Bit;
            this.imageListIcons.ImageSize = new System.Drawing.Size(32, 32);
            this.imageListIcons.TransparentColor = System.Drawing.Color.Transparent;
            // 
            // pictureBoxPreview
            // 
            this.pictureBoxPreview.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pictureBoxPreview.Location = new System.Drawing.Point(372, 14);
            this.pictureBoxPreview.Name = "pictureBoxPreview";
            this.pictureBoxPreview.Size = new System.Drawing.Size(48, 48);
            this.pictureBoxPreview.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
            this.pictureBoxPreview.TabIndex = 11;
            this.pictureBoxPreview.TabStop = false;
            // 
            // FormCreateItem
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(462, 179);
            this.Controls.Add(this.pictureBoxPreview);
            this.Controls.Add(this.listViewIcons);
            this.Controls.Add(this.buttonIconBrowser);
            this.Controls.Add(this.textBoxIconPath);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.buttonBrowser);
            this.Controls.Add(this.textBoxPath);
            this.Controls.Add(this.textBoxName);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.buttonOK);
            this.Controls.Add(this.button1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FormCreateItem";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "New Shortcut";
            this.Load += new System.EventHandler(this.FormCreateItem_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxPreview)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button buttonOK;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox textBoxName;
        private System.Windows.Forms.TextBox textBoxPath;
        private System.Windows.Forms.Button buttonBrowser;
        private System.Windows.Forms.OpenFileDialog openFileDialogPath;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox textBoxIconPath;
        private System.Windows.Forms.Button buttonIconBrowser;
        private System.Windows.Forms.OpenFileDialog openFileDialogIcon;
        private System.Windows.Forms.ListView listViewIcons;
        private System.Windows.Forms.ImageList imageListIcons;
        private System.Windows.Forms.PictureBox pictureBoxPreview;
    }
}