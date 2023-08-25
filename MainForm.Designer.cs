namespace WACCALauncher
{
    partial class MainForm
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
            this.menuLabel = new System.Windows.Forms.Label();
            this.waccaListTest = new WACCALauncher.WaccaList();
            this.SuspendLayout();
            // 
            // menuLabel
            // 
            this.menuLabel.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.menuLabel.Font = new System.Drawing.Font("Tahoma", 22.5F);
            this.menuLabel.ForeColor = System.Drawing.Color.White;
            this.menuLabel.Location = new System.Drawing.Point(287, 127);
            this.menuLabel.Name = "menuLabel";
            this.menuLabel.Size = new System.Drawing.Size(500, 30);
            this.menuLabel.TabIndex = 0;
            this.menuLabel.Text = "MENU TITLE";
            this.menuLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.menuLabel.Visible = false;
            // 
            // waccaListTest
            // 
            this.waccaListTest.BackColor = System.Drawing.Color.Black;
            this.waccaListTest.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.waccaListTest.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawVariable;
            this.waccaListTest.Enabled = false;
            this.waccaListTest.ForeColor = System.Drawing.Color.White;
            this.waccaListTest.FormattingEnabled = true;
            this.waccaListTest.ItemHeight = 40;
            this.waccaListTest.Location = new System.Drawing.Point(200, 240);
            this.waccaListTest.Name = "waccaListTest";
            this.waccaListTest.Size = new System.Drawing.Size(680, 600);
            this.waccaListTest.TabIndex = 1;
            this.waccaListTest.Visible = false;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Black;
            this.ClientSize = new System.Drawing.Size(1080, 1080);
            this.Controls.Add(this.waccaListTest);
            this.Controls.Add(this.menuLabel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.KeyPreview = true;
            this.Name = "MainForm";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "WACCA Launcher";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label menuLabel;
        private WaccaList waccaListTest;
    }
}

