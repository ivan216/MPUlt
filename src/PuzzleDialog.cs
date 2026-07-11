using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace _3dedit {
    public partial class PuzzleDialog : Form {
        Hashtable m_puzzleList;
        string m_puzzleFilePath;

        public PuzzleDialog(Hashtable puzzleList, string puzzleFilePath) {
            m_puzzleList = puzzleList;
            m_puzzleFilePath = puzzleFilePath;
            InitializeComponent();

            // Populate block dropdown
            PopulateBlockList();

            // Show available puzzle count
            var all = PuzzleFileUtils.GetAllPuzzles(m_puzzleList);
            m_btnLoadExisting.Enabled = all.Count > 0;
        }

        // ---- Properties for return values ----
        public string PuzzleName {
            get { return Sanitize(txtName.Text); }
        }

        public string BlockPath {
            get {
                string t = Sanitize(cmbBlock.Text);
                if (t == "(Root)" || t == "") return "";
                return t;
            }
        }

        static string Sanitize(string s) {
            return s.Trim().Replace(' ', '_');
        }

        public string[] DefinitionLines {
            get {
                string text = txtDefinition.Text.Trim();
                if (string.IsNullOrEmpty(text)) return new string[0];
                var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                // Trim each line and remove blank lines
                var result = new List<string>();
                foreach (var ln in lines) {
                    string t = ln.Trim();
                    if (t.Length > 0) result.Add(t);
                }
                return result.ToArray();
            }
        }

        public bool LoadAfterSave { get; private set; }

        // ---- Button events ----

        private void m_btnTest_Click(object sender, EventArgs e) {
            string name = PuzzleName;
            string[] def = DefinitionLines;
            string blockPath = BlockPath;
            if (string.IsNullOrEmpty(name)) {
                MessageBox.Show(this, "Please enter a puzzle name.", "Test", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (def.Length == 0) {
                MessageBox.Show(this, "Please enter puzzle definition text.", "Test", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try {
                PuzzleStructure pstr = PuzzleStructure.Create(name, def);
                // Success: show basic info
                int nAxes = pstr.Axes.Length;
                int nFaces = pstr.Faces.Length;
                int nStickers = pstr.NStickers;
                int dim = pstr.Dim;
                string msg = string.Format("Puzzle \"{0}\" is valid!\n\nDim: {1}\nAxes: {2}\nFaces: {3}\nStickers: {4}",
                    name, dim, nAxes, nFaces, nStickers);
                // Check for duplicate name in target block
                if (PuzzleFileUtils.PuzzleExistsInBlock(m_puzzleList, name, blockPath)) {
                    msg += "\n\n⚠ Duplicate: a puzzle named \"" + name + "\" already exists in \"" + blockPath + "\"";
                } else {
                    msg += "\n\n✓ No duplicate name in \"" + blockPath + "\"";
                }
                MessageBox.Show(this, msg, "Test Passed", MessageBoxButtons.OK, MessageBoxIcon.Information);
            } catch (Exception ex) {
                MessageBox.Show(this, "Puzzle creation failed:\n" + ex.Message,
                    "Test Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void m_btnGenSave_Click(object sender, EventArgs e) {
            LoadAfterSave = true;
            if (DoSave(true)) {
                DialogResult = DialogResult.OK;
                Close();
            }
        }

        private void m_btnSaveOnly_Click(object sender, EventArgs e) {
            LoadAfterSave = false;
            if (DoSave(false)) {
                DialogResult = DialogResult.OK;
                Close();
            }
        }

        private void m_btnLoadExisting_Click(object sender, EventArgs e) {
            using (var selector = new PuzzleSelectorDialog(m_puzzleList, PuzzleSelectorMode.Load)) {
                if (selector.ShowDialog(this) == DialogResult.OK) {
                    // Fill in the fields
                    txtName.Text = selector.SelectedPuzzleName;
                    string bp = selector.SelectedBlockPath;
                    cmbBlock.Text = string.IsNullOrEmpty(bp) ? "(Root)" : bp;
                    // Load definition text from the file to get the raw text
                    string rawText = PuzzleFileUtils.GetPuzzleText(m_puzzleFilePath, selector.SelectedPuzzleName);
                    if (rawText != null) {
                        txtDefinition.Text = rawText;
                    } else {
                        // Fallback: reconstruct from stored definition
                        var allPuzzles = PuzzleFileUtils.GetAllPuzzles(m_puzzleList);
                        var entry = allPuzzles.Find(pe => pe.Name == selector.SelectedPuzzleName);
                        if (entry.Definition != null) {
                            txtDefinition.Text = string.Join(Environment.NewLine, entry.Definition);
                        }
                    }
                }
            }
        }

        private void m_btnCancel_Click(object sender, EventArgs e) {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        // ---- Helpers ----

        bool DoSave(bool fullValidation) {
            string name = PuzzleName;
            string[] def = DefinitionLines;
            string blockPath = BlockPath;

            if (string.IsNullOrEmpty(name)) {
                MessageBox.Show(this, "Please enter a puzzle name.", "Save", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            if (def.Length == 0) {
                MessageBox.Show(this, "Please enter puzzle definition text.", "Save", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            // Lightweight: validate definition format only (parses Dim/NAxis/Faces/Group/Axis/Twists/Cuts).
            if (!PuzzleStructure.ValidateDefinitionFormat(def)) {
                MessageBox.Show(this, "Invalid puzzle definition format — check syntax.\n" +
                    "Required: Dim, NAxis, Faces, Group, Axis, Twists, Cuts",
                    "Format Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            PuzzleStructure pstr = null;
            if (fullValidation) {
                // Full validation: try to actually create the puzzle structure.
                try {
                    pstr = PuzzleStructure.Create(name, def);
                } catch (Exception ex) {
                    MessageBox.Show(this, "Puzzle creation failed — not saved.\n" + ex.Message,
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
            }

            // Check for duplicate name in the same block
            if (PuzzleFileUtils.PuzzleExistsInBlock(m_puzzleList, name, blockPath)) {
                var res = MessageBox.Show(this,
                    string.Format("A puzzle named \"{0}\" already exists in block \"{1}\". Overwrite?", name, blockPath),
                    "Duplicate Name", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (res != DialogResult.Yes) return false;
                // Delete the existing one from the same block first
                PuzzleFileUtils.RemovePuzzleFromBlock(m_puzzleFilePath, name, blockPath);
            }

            // Save to file
            if (!PuzzleFileUtils.AddPuzzle(m_puzzleFilePath, name, def, blockPath)) {
                MessageBox.Show(this, "Failed to write to puzzles file: " + m_puzzleFilePath,
                    "File Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            // Store the created structure for the caller to load
            m_createdStructure = pstr;
            return true;
        }

        void PopulateBlockList() {
            cmbBlock.Items.Clear();
            cmbBlock.Items.Add("(Root)");
            var blocks = PuzzleFileUtils.GetAllBlockPaths(m_puzzleList);
            blocks.Sort();
            foreach (var b in blocks) {
                cmbBlock.Items.Add(b);
            }
            cmbBlock.SelectedIndex = 0; // default: Root
        }

        // The created PuzzleStructure, for the caller to load
        internal PuzzleStructure CreatedStructure { get { return m_createdStructure; } }
        PuzzleStructure m_createdStructure = null;
    }
}
