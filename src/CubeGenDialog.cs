using System;
using System.Windows.Forms;

namespace _3dedit {
    public partial class CubeGenDialog : Form {
        public CubeGenDialog() {
            InitializeComponent();
        }

        public int PuzzleDimension { get { return (int)nudDim.Value; } }
        public int PuzzleOrder { get { return (int)nudOrder.Value; } }
        public string PuzzleType { get { return cmbType.SelectedItem.ToString(); } }

        private void InitializeComponent() {
            this.lblDesc = new Label();
            this.lblType = new Label();
            this.cmbType = new ComboBox();
            this.lblDim = new Label();
            this.nudDim = new NumericUpDown();
            this.lblOrder = new Label();
            this.nudOrder = new NumericUpDown();
            this.m_btnGen = new Button();
            this.m_btnCancel = new Button();
            ((System.ComponentModel.ISupportInitialize)(this.nudDim)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudOrder)).BeginInit();
            this.SuspendLayout();

            // lblDesc
            this.lblDesc.Text = "Generate a twisty puzzle.\nHigh D uses direct mesh generation (no OOM).";
            this.lblDesc.Location = new System.Drawing.Point(12, 12);
            this.lblDesc.Size = new System.Drawing.Size(260, 30);

            // lblType
            this.lblType.Text = "Type:";
            this.lblType.Location = new System.Drawing.Point(12, 50);
            this.lblType.Size = new System.Drawing.Size(60, 23);

            // cmbType
            this.cmbType.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cmbType.Items.AddRange(new object[] { "CubeBased N^D", "Cross-polytope FT" });
            this.cmbType.SelectedIndex = 0;
            this.cmbType.Location = new System.Drawing.Point(70, 48);
            this.cmbType.Size = new System.Drawing.Size(160, 21);
            this.cmbType.SelectedIndexChanged += new EventHandler(cmbType_SelectedIndexChanged);

            // lblDim
            this.lblDim.Text = "Dimension (D):";
            this.lblDim.Location = new System.Drawing.Point(12, 78);
            this.lblDim.Size = new System.Drawing.Size(100, 23);

            // nudDim
            this.nudDim.Minimum = 4;
            this.nudDim.Maximum = 15;
            this.nudDim.Value = 4;
            this.nudDim.Location = new System.Drawing.Point(120, 76);
            this.nudDim.Size = new System.Drawing.Size(60, 20);

            // lblOrder
            this.lblOrder.Text = "Order (N):";
            this.lblOrder.Location = new System.Drawing.Point(12, 104);
            this.lblOrder.Size = new System.Drawing.Size(100, 23);

            // nudOrder
            this.nudOrder.Minimum = 1;
            this.nudOrder.Maximum = 9;
            this.nudOrder.Value = 2;
            this.nudOrder.Location = new System.Drawing.Point(120, 102);
            this.nudOrder.Size = new System.Drawing.Size(60, 20);

            // m_btnGen
            this.m_btnGen.Text = "Generate";
            this.m_btnGen.Location = new System.Drawing.Point(12, 136);
            this.m_btnGen.Size = new System.Drawing.Size(90, 26);
            this.m_btnGen.Click += new EventHandler(this.m_btnGen_Click);

            // m_btnCancel
            this.m_btnCancel.Text = "Cancel";
            this.m_btnCancel.Location = new System.Drawing.Point(110, 136);
            this.m_btnCancel.Size = new System.Drawing.Size(90, 26);
            this.m_btnCancel.Click += new EventHandler(this.m_btnCancel_Click);

            // CubeGenDialog
            this.ClientSize = new System.Drawing.Size(250, 178);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "Puzzle Generator";
            this.Controls.Add(this.lblDesc);
            this.Controls.Add(this.lblType);
            this.Controls.Add(this.cmbType);
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

        private Label lblDesc, lblType, lblDim, lblOrder;
        private ComboBox cmbType;
        private NumericUpDown nudDim, nudOrder;
        private Button m_btnGen, m_btnCancel;

        internal PuzzleStructure CreatedStructure;

        private void cmbType_SelectedIndexChanged(object sender, EventArgs e) {
            nudDim.Maximum = 15;
        }

        private void m_btnGen_Click(object sender, EventArgs e) {
            int dim = PuzzleDimension;
            int order = PuzzleOrder;
            try {
                if (cmbType.SelectedIndex == 0)
                    CreatedStructure = PuzzleStructure.CreateCubeGenerated(dim, order);
                else
                    CreatedStructure = PuzzleStructure.CreateCrossGenerated(dim, order);
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
