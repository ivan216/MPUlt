namespace _3dedit {
    partial class PuzzleDialog {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent() {
            this.lblName = new System.Windows.Forms.Label();
            this.txtName = new System.Windows.Forms.TextBox();
            this.lblBlock = new System.Windows.Forms.Label();
            this.cmbBlock = new System.Windows.Forms.ComboBox();
            this.lblDefinition = new System.Windows.Forms.Label();
            this.txtDefinition = new System.Windows.Forms.TextBox();
            this.m_btnLoadExisting = new System.Windows.Forms.Button();
            this.m_btnTest = new System.Windows.Forms.Button();
            this.m_btnGenSave = new System.Windows.Forms.Button();
            this.m_btnSaveOnly = new System.Windows.Forms.Button();
            this.m_btnCancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            //
            // lblName
            //
            this.lblName.AutoSize = true;
            this.lblName.Location = new System.Drawing.Point(12, 15);
            this.lblName.Size = new System.Drawing.Size(77, 13);
            this.lblName.Text = "Puzzle Name:";
            //
            // txtName
            //
            this.txtName.Location = new System.Drawing.Point(100, 12);
            this.txtName.Size = new System.Drawing.Size(280, 20);
            //
            // lblBlock
            //
            this.lblBlock.AutoSize = true;
            this.lblBlock.Location = new System.Drawing.Point(12, 41);
            this.lblBlock.Size = new System.Drawing.Size(82, 13);
            this.lblBlock.Text = "Save to Block:";
            //
            // cmbBlock
            //
            this.cmbBlock.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDown; // editable
            this.cmbBlock.Location = new System.Drawing.Point(100, 38);
            this.cmbBlock.Size = new System.Drawing.Size(280, 21);
            //
            // lblDefinition
            //
            this.lblDefinition.AutoSize = true;
            this.lblDefinition.Location = new System.Drawing.Point(12, 70);
            this.lblDefinition.Size = new System.Drawing.Size(120, 13);
            this.lblDefinition.Text = "Puzzle Definition Text:";
            //
            // txtDefinition
            //
            this.txtDefinition.AcceptsReturn = true;
            this.txtDefinition.AcceptsTab = true;
            this.txtDefinition.Font = new System.Drawing.Font("Consolas", 9.75F);
            this.txtDefinition.Location = new System.Drawing.Point(12, 86);
            this.txtDefinition.Multiline = true;
            this.txtDefinition.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtDefinition.Size = new System.Drawing.Size(560, 283);
            this.txtDefinition.WordWrap = false;
            //
            // m_btnLoadExisting
            //
            this.m_btnLoadExisting.Location = new System.Drawing.Point(12, 380);
            this.m_btnLoadExisting.Size = new System.Drawing.Size(110, 26);
            this.m_btnLoadExisting.Text = "Load Existing...";
            this.m_btnLoadExisting.Click += new System.EventHandler(this.m_btnLoadExisting_Click);
            //
            // m_btnTest
            //
            this.m_btnTest.Location = new System.Drawing.Point(128, 380);
            this.m_btnTest.Size = new System.Drawing.Size(80, 26);
            this.m_btnTest.Text = "Test";
            this.m_btnTest.Click += new System.EventHandler(this.m_btnTest_Click);
            //
            // m_btnGenSave
            //
            this.m_btnGenSave.Location = new System.Drawing.Point(290, 380);
            this.m_btnGenSave.Size = new System.Drawing.Size(120, 26);
            this.m_btnGenSave.Text = "Generate && Save";
            this.m_btnGenSave.Click += new System.EventHandler(this.m_btnGenSave_Click);
            //
            // m_btnSaveOnly
            //
            this.m_btnSaveOnly.Location = new System.Drawing.Point(416, 380);
            this.m_btnSaveOnly.Size = new System.Drawing.Size(80, 26);
            this.m_btnSaveOnly.Text = "Save Only";
            this.m_btnSaveOnly.Click += new System.EventHandler(this.m_btnSaveOnly_Click);
            //
            // m_btnCancel
            //
            this.m_btnCancel.Location = new System.Drawing.Point(502, 380);
            this.m_btnCancel.Size = new System.Drawing.Size(70, 26);
            this.m_btnCancel.Text = "Cancel";
            this.m_btnCancel.Click += new System.EventHandler(this.m_btnCancel_Click);
            //
            // PuzzleDialog
            //
            this.AcceptButton = this.m_btnGenSave;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.m_btnCancel;
            this.ClientSize = new System.Drawing.Size(584, 416);
            this.Controls.Add(this.m_btnCancel);
            this.Controls.Add(this.m_btnSaveOnly);
            this.Controls.Add(this.m_btnGenSave);
            this.Controls.Add(this.m_btnTest);
            this.Controls.Add(this.m_btnLoadExisting);
            this.Controls.Add(this.txtDefinition);
            this.Controls.Add(this.lblDefinition);
            this.Controls.Add(this.cmbBlock);
            this.Controls.Add(this.lblBlock);
            this.Controls.Add(this.txtName);
            this.Controls.Add(this.lblName);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "PuzzleDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Create Custom Puzzle";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.Label lblName;
        private System.Windows.Forms.TextBox txtName;
        private System.Windows.Forms.Label lblBlock;
        private System.Windows.Forms.ComboBox cmbBlock;
        private System.Windows.Forms.Label lblDefinition;
        private System.Windows.Forms.TextBox txtDefinition;
        private System.Windows.Forms.Button m_btnLoadExisting;
        private System.Windows.Forms.Button m_btnTest;
        private System.Windows.Forms.Button m_btnGenSave;
        private System.Windows.Forms.Button m_btnSaveOnly;
        private System.Windows.Forms.Button m_btnCancel;
    }
}
