namespace _3dedit {
    partial class PuzzleSelectorDialog {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent() {
            this.treePuzzles = new System.Windows.Forms.TreeView();
            this.m_btnOk = new System.Windows.Forms.Button();
            this.m_btnDelete = new System.Windows.Forms.Button();
            this.m_btnMove = new System.Windows.Forms.Button();
            this.m_btnCancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            //
            // treePuzzles
            //
            this.treePuzzles.Location = new System.Drawing.Point(12, 12);
            this.treePuzzles.Size = new System.Drawing.Size(360, 340);
            this.treePuzzles.TabIndex = 0;
            this.treePuzzles.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.treePuzzles_AfterSelect);
            this.treePuzzles.DoubleClick += new System.EventHandler(this.treePuzzles_DoubleClick);
            //
            // m_btnOk
            //
            this.m_btnOk.Enabled = false;
            this.m_btnOk.Location = new System.Drawing.Point(12, 362);
            this.m_btnOk.Size = new System.Drawing.Size(80, 26);
            this.m_btnOk.TabIndex = 1;
            this.m_btnOk.Text = "OK";
            this.m_btnOk.Click += new System.EventHandler(this.m_btnOk_Click);
            //
            // m_btnDelete
            //
            this.m_btnDelete.Enabled = false;
            this.m_btnDelete.Location = new System.Drawing.Point(12, 362);
            this.m_btnDelete.Size = new System.Drawing.Size(80, 26);
            this.m_btnDelete.TabIndex = 2;
            this.m_btnDelete.Text = "Delete";
            this.m_btnDelete.Click += new System.EventHandler(this.m_btnDelete_Click);
            //
            // m_btnMove
            //
            this.m_btnMove.Enabled = false;
            this.m_btnMove.Location = new System.Drawing.Point(98, 362);
            this.m_btnMove.Size = new System.Drawing.Size(80, 26);
            this.m_btnMove.TabIndex = 3;
            this.m_btnMove.Text = "Move...";
            this.m_btnMove.Click += new System.EventHandler(this.m_btnMove_Click);
            //
            // m_btnCancel
            //
            this.m_btnCancel.Location = new System.Drawing.Point(292, 362);
            this.m_btnCancel.Size = new System.Drawing.Size(80, 26);
            this.m_btnCancel.TabIndex = 4;
            this.m_btnCancel.Text = "Cancel";
            this.m_btnCancel.Click += new System.EventHandler(this.m_btnCancel_Click);
            //
            // PuzzleSelectorDialog
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(384, 398);
            this.Controls.Add(this.m_btnCancel);
            this.Controls.Add(this.m_btnMove);
            this.Controls.Add(this.m_btnDelete);
            this.Controls.Add(this.m_btnOk);
            this.Controls.Add(this.treePuzzles);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "PuzzleSelectorDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Select Puzzle";
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.TreeView treePuzzles;
        private System.Windows.Forms.Button m_btnOk;
        private System.Windows.Forms.Button m_btnDelete;
        private System.Windows.Forms.Button m_btnMove;
        private System.Windows.Forms.Button m_btnCancel;
    }
}
