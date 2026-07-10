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
            this.lblDesc = new System.Windows.Forms.Label();
            this.lblType = new System.Windows.Forms.Label();
            this.cmbType = new System.Windows.Forms.ComboBox();
            this.lblDim = new System.Windows.Forms.Label();
            this.nudDim = new System.Windows.Forms.NumericUpDown();
            this.lblOrder = new System.Windows.Forms.Label();
            this.nudOrder = new System.Windows.Forms.NumericUpDown();
            this.m_btnGen = new System.Windows.Forms.Button();
            this.m_btnCancel = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.nudDim)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudOrder)).BeginInit();
            this.SuspendLayout();
            // 
            // lblDesc
            // 
            this.lblDesc.Location = new System.Drawing.Point(12, 12);
            this.lblDesc.Name = "lblDesc";
            this.lblDesc.Size = new System.Drawing.Size(289, 33);
            this.lblDesc.TabIndex = 0;
            this.lblDesc.Text = "Generate a twisty puzzle";
            // 
            // lblType
            // 
            this.lblType.Location = new System.Drawing.Point(12, 56);
            this.lblType.Name = "lblType";
            this.lblType.Size = new System.Drawing.Size(92, 23);
            this.lblType.TabIndex = 1;
            this.lblType.Text = "Type:";
            // 
            // cmbType
            // 
            this.cmbType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbType.Items.AddRange(new object[] {
            "CubeBased N^D",
            "Cross-polytope FT",
            "Simplex FT"});
            this.cmbType.Location = new System.Drawing.Point(144, 53);
            this.cmbType.Name = "cmbType";
            this.cmbType.Size = new System.Drawing.Size(205, 29);
            this.cmbType.TabIndex = 2;
            this.cmbType.SelectedIndexChanged += new System.EventHandler(this.cmbType_SelectedIndexChanged);
            // 
            // lblDim
            // 
            this.lblDim.Location = new System.Drawing.Point(12, 100);
            this.lblDim.Name = "lblDim";
            this.lblDim.Size = new System.Drawing.Size(189, 29);
            this.lblDim.TabIndex = 3;
            this.lblDim.Text = "Dimension (D):";
            // 
            // nudDim
            // 
            this.nudDim.Location = new System.Drawing.Point(289, 98);
            this.nudDim.Maximum = new decimal(new int[] {
            15,
            0,
            0,
            0});
            this.nudDim.Minimum = new decimal(new int[] {
            4,
            0,
            0,
            0});
            this.nudDim.Name = "nudDim";
            this.nudDim.Size = new System.Drawing.Size(60, 31);
            this.nudDim.TabIndex = 4;
            this.nudDim.Value = new decimal(new int[] {
            4,
            0,
            0,
            0});
            // 
            // lblOrder
            // 
            this.lblOrder.Location = new System.Drawing.Point(12, 146);
            this.lblOrder.Name = "lblOrder";
            this.lblOrder.Size = new System.Drawing.Size(189, 23);
            this.lblOrder.TabIndex = 5;
            this.lblOrder.Text = "Order (N):";
            // 
            // nudOrder
            // 
            this.nudOrder.Location = new System.Drawing.Point(289, 144);
            this.nudOrder.Maximum = new decimal(new int[] {
            9,
            0,
            0,
            0});
            this.nudOrder.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nudOrder.Name = "nudOrder";
            this.nudOrder.Size = new System.Drawing.Size(60, 31);
            this.nudOrder.TabIndex = 6;
            this.nudOrder.Value = new decimal(new int[] {
            2,
            0,
            0,
            0});
            // 
            // m_btnGen
            // 
            this.m_btnGen.Location = new System.Drawing.Point(228, 190);
            this.m_btnGen.Name = "m_btnGen";
            this.m_btnGen.Size = new System.Drawing.Size(121, 40);
            this.m_btnGen.TabIndex = 7;
            this.m_btnGen.Text = "Generate";
            this.m_btnGen.Click += new System.EventHandler(this.m_btnGen_Click);
            // 
            // m_btnCancel
            // 
            this.m_btnCancel.Location = new System.Drawing.Point(16, 190);
            this.m_btnCancel.Name = "m_btnCancel";
            this.m_btnCancel.Size = new System.Drawing.Size(121, 40);
            this.m_btnCancel.TabIndex = 8;
            this.m_btnCancel.Text = "Cancel";
            this.m_btnCancel.Click += new System.EventHandler(this.m_btnCancel_Click);
            // 
            // CubeGenDialog
            // 
            this.ClientSize = new System.Drawing.Size(369, 245);
            this.Controls.Add(this.lblDesc);
            this.Controls.Add(this.lblType);
            this.Controls.Add(this.cmbType);
            this.Controls.Add(this.lblDim);
            this.Controls.Add(this.nudDim);
            this.Controls.Add(this.lblOrder);
            this.Controls.Add(this.nudOrder);
            this.Controls.Add(this.m_btnGen);
            this.Controls.Add(this.m_btnCancel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "CubeGenDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Puzzle Generator";
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
                else if (cmbType.SelectedIndex == 1)
                    CreatedStructure = PuzzleStructure.CreateCrossGenerated(dim, order);
                else
                    CreatedStructure = PuzzleStructure.CreateSimplexGenerated(dim, order);
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
