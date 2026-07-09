using System;
using System.Windows.Forms;

namespace _3dedit {
    public partial class CubeGenDialog : Form {
        public CubeGenDialog() {
            InitializeComponent();
        }

        public int PuzzleDimension { get { return (int)nudDim.Value; } }
        public int PuzzleOrder { get { return (int)nudOrder.Value; } }

        private void InitializeComponent() {
            this.lblDim = new Label();
            this.nudDim = new NumericUpDown();
            this.lblOrder = new Label();
            this.nudOrder = new NumericUpDown();
            this.lblDesc = new Label();
            this.m_btnGen = new Button();
            this.m_btnCancel = new Button();
            ((System.ComponentModel.ISupportInitialize)(this.nudDim)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudOrder)).BeginInit();
            this.SuspendLayout();

            // lblDesc
            this.lblDesc.Text = "Generate a CubeBased N^D twisty puzzle.\n"
                + "Order 1 = single piece (cuts outside).\n"
                + "High D uses direct mesh generation (no OOM).";
            this.lblDesc.Location = new System.Drawing.Point(12, 12);
            this.lblDesc.Size = new System.Drawing.Size(320, 40);

            // lblDim
            this.lblDim.Text = "Dimension (D):";
            this.lblDim.Location = new System.Drawing.Point(12, 60);
            this.lblDim.Size = new System.Drawing.Size(100, 23);

            // nudDim
            this.nudDim.Minimum = 4;
            this.nudDim.Maximum = 15;
            this.nudDim.Value = 4;
            this.nudDim.Location = new System.Drawing.Point(120, 58);
            this.nudDim.Size = new System.Drawing.Size(60, 20);

            // lblOrder
            this.lblOrder.Text = "Order (N):";
            this.lblOrder.Location = new System.Drawing.Point(12, 88);
            this.lblOrder.Size = new System.Drawing.Size(100, 23);

            // nudOrder
            this.nudOrder.Minimum = 1;
            this.nudOrder.Maximum = 9;
            this.nudOrder.Value = 2;
            this.nudOrder.Location = new System.Drawing.Point(120, 86);
            this.nudOrder.Size = new System.Drawing.Size(60, 20);

            // m_btnGen
            this.m_btnGen.Text = "Generate";
            this.m_btnGen.Location = new System.Drawing.Point(12, 120);
            this.m_btnGen.Size = new System.Drawing.Size(90, 26);
            this.m_btnGen.Click += new EventHandler(this.m_btnGen_Click);

            // m_btnCancel
            this.m_btnCancel.Text = "Cancel";
            this.m_btnCancel.Location = new System.Drawing.Point(110, 120);
            this.m_btnCancel.Size = new System.Drawing.Size(90, 26);
            this.m_btnCancel.Click += new EventHandler(this.m_btnCancel_Click);

            // CubeGenDialog
            this.ClientSize = new System.Drawing.Size(220, 160);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "Cube Generator";
            this.Controls.Add(this.lblDesc);
            this.Controls.Add(this.lblDim);
            this.Controls.Add(this.nudDim);
            this.Controls.Add(this.lblOrder);
            this.Controls.Add(this.nudOrder);
            this.Controls.Add(this.m_btnGen);
            this.Controls.Add(this.m_btnCancel);

            ((System.ComponentModel.ISupportInitialize)(this.nudDim)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudOrder)).EndInit();
            this.ResumeLayout(false);
        }

        private Label lblDim;
        private NumericUpDown nudDim;
        private Label lblOrder;
        private NumericUpDown nudOrder;
        private Label lblDesc;
        private Button m_btnGen;
        private Button m_btnCancel;

        internal PuzzleStructure CreatedStructure;

        private void m_btnGen_Click(object sender, EventArgs e) {
            int dim = PuzzleDimension;
            int order = PuzzleOrder;
            try {
                CreatedStructure = PuzzleStructure.CreateCubeGenerated(dim, order);
                DialogResult = DialogResult.OK;
                Close();
            } catch (Exception ex) {
                MessageBox.Show(this, "Generation failed:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void m_btnCancel_Click(object sender, EventArgs e) {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
