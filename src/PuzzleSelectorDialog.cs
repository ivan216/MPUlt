using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace _3dedit {

    public enum PuzzleSelectorMode {
        Load,   // OK/Cancel, returns selected puzzle
        Delete  // Has Delete/Move buttons plus Cancel
    }

    public partial class PuzzleSelectorDialog : Form {
        Hashtable m_puzzleList;
        PuzzleSelectorMode m_mode;

        public PuzzleSelectorDialog(Hashtable puzzleList, PuzzleSelectorMode mode) {
            m_puzzleList = puzzleList;
            m_mode = mode;
            InitializeComponent();

            if (mode == PuzzleSelectorMode.Delete) {
                this.Text = "Delete / Move Puzzle";
                m_btnOk.Visible = false;
                m_btnDelete.Visible = true;
                m_btnMove.Visible = true;
            } else {
                this.Text = "Select Puzzle";
                m_btnDelete.Visible = false;
                m_btnMove.Visible = false;
                m_btnOk.Visible = true;
            }

            PopulateTree();
        }

        public string SelectedPuzzleName { get; private set; }
        public string SelectedBlockPath { get; private set; }

        // Events the caller can subscribe to for delete/move
        public event Action<string, string> DeleteRequested; // name, blockPath
        public event Action<string, string, string> MoveRequested; // name, sourceBlockPath, targetBlockPath

        void PopulateTree() {
            treePuzzles.Nodes.Clear();
            foreach (DictionaryEntry de in m_puzzleList) {
                string key = (string)de.Key;
                if (de.Value is Hashtable) {
                    TreeNode node = new TreeNode(key);
                    AddBlockNodes(node, (Hashtable)de.Value, key);
                    if (node.Nodes.Count > 0)
                        treePuzzles.Nodes.Add(node);
                } else if (de.Value is string[]) {
                    TreeNode node = new TreeNode(key);
                    node.Tag = new PuzzleEntry(key, (string[])de.Value, "");
                    treePuzzles.Nodes.Add(node);
                }
            }
            treePuzzles.ExpandAll();
        }

        void AddBlockNodes(TreeNode parent, Hashtable block, string parentPath) {
            var keys = new ArrayList(block.Keys);
            keys.Sort();
            foreach (string key in keys) {
                object val = block[key];
                if (val is Hashtable) {
                    TreeNode node = new TreeNode(key);
                    string childPath = parentPath + "/" + key;
                    AddBlockNodes(node, (Hashtable)val, childPath);
                    if (node.Nodes.Count > 0)
                        parent.Nodes.Add(node);
                } else if (val is string[]) {
                    TreeNode node = new TreeNode(key);
                    node.Tag = new PuzzleEntry(key, (string[])val, parentPath);
                    parent.Nodes.Add(node);
                }
            }
        }

        PuzzleEntry? GetSelectedEntry() {
            TreeNode sel = treePuzzles.SelectedNode;
            if (sel == null) return null;
            if (sel.Tag is PuzzleEntry) {
                return (PuzzleEntry)sel.Tag;
            }
            // If a block node is selected, find first leaf
            return null;
        }

        private void treePuzzles_AfterSelect(object sender, TreeViewEventArgs e) {
            var entry = GetSelectedEntry();
            bool valid = entry.HasValue;
            if (m_mode == PuzzleSelectorMode.Load) {
                m_btnOk.Enabled = valid;
            } else {
                m_btnDelete.Enabled = valid;
                m_btnMove.Enabled = valid;
            }
        }

        private void treePuzzles_DoubleClick(object sender, EventArgs e) {
            if (m_mode == PuzzleSelectorMode.Load && m_btnOk.Enabled) {
                m_btnOk_Click(sender, e);
            }
        }

        private void m_btnOk_Click(object sender, EventArgs e) {
            var entry = GetSelectedEntry();
            if (entry.HasValue) {
                SelectedPuzzleName = entry.Value.Name;
                SelectedBlockPath = entry.Value.BlockPath;
                DialogResult = DialogResult.OK;
                Close();
            }
        }

        private void m_btnDelete_Click(object sender, EventArgs e) {
            var entry = GetSelectedEntry();
            if (!entry.HasValue) return;

            string name = entry.Value.Name;
            var res = MessageBox.Show(this,
                string.Format("Are you sure you want to delete puzzle \"{0}\"?\nThis cannot be undone.", name),
                "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (res != DialogResult.Yes) return;

            if (DeleteRequested != null)
                DeleteRequested(name, entry.Value.BlockPath);

            // Remove from tree
            TreeNode sel = treePuzzles.SelectedNode;
            treePuzzles.Nodes.Remove(sel);
            m_btnDelete.Enabled = false;
            m_btnMove.Enabled = false;
        }

        private void m_btnMove_Click(object sender, EventArgs e) {
            var entry = GetSelectedEntry();
            if (!entry.HasValue) return;

            // Show block selection dialog with combo box
            using (BlockInputDialog dlg = new BlockInputDialog(m_puzzleList, entry.Value.BlockPath)) {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                string target = dlg.BlockPath;

                if (MoveRequested != null)
                    MoveRequested(entry.Value.Name, entry.Value.BlockPath, target);
            }

            // Close after move
            Close();
        }

        private void m_btnCancel_Click(object sender, EventArgs e) {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }

    // ---- Block path input dialog with dropdown + free text ----
    class BlockInputDialog : Form {
        ComboBox cmbBlock;
        Button btnOk;
        Button btnCancel;

        public BlockInputDialog(Hashtable puzzleList, string currentPath) {
            this.Text = "Move Puzzle: select target block";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.ClientSize = new System.Drawing.Size(320, 100);
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ControlBox = false;

            Label lbl = new Label();
            lbl.Text = "Target block (select or type custom path):";
            lbl.Location = new System.Drawing.Point(12, 10);
            lbl.Size = new System.Drawing.Size(296, 16);

            cmbBlock = new ComboBox();
            cmbBlock.DropDownStyle = ComboBoxStyle.DropDown; // editable
            cmbBlock.Location = new System.Drawing.Point(12, 30);
            cmbBlock.Size = new System.Drawing.Size(296, 22);
            // Populate with existing blocks
            cmbBlock.Items.Add("(Root)");
            var blocks = PuzzleFileUtils.GetAllBlockPaths(puzzleList);
            blocks.Sort();
            foreach (var b in blocks) cmbBlock.Items.Add(b);
            // Set current value
            if (string.IsNullOrEmpty(currentPath))
                cmbBlock.Text = "(Root)";
            else
                cmbBlock.Text = currentPath;

            btnOk = new Button();
            btnOk.Text = "Move";
            btnOk.DialogResult = DialogResult.OK;
            btnOk.Location = new System.Drawing.Point(150, 65);
            btnOk.Size = new System.Drawing.Size(75, 23);

            btnCancel = new Button();
            btnCancel.Text = "Cancel";
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Location = new System.Drawing.Point(233, 65);
            btnCancel.Size = new System.Drawing.Size(75, 23);

            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;

            this.Controls.Add(lbl);
            this.Controls.Add(cmbBlock);
            this.Controls.Add(btnOk);
            this.Controls.Add(btnCancel);
        }

        static string Sanitize(string s) {
            return s.Trim().Replace(' ', '_');
        }

        public string BlockPath {
            get {
                string t = Sanitize(cmbBlock.Text);
                if (t == "(Root)" || t == "") return "";
                return t;
            }
        }
    }
}
