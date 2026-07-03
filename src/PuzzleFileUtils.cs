using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace _3dedit {

    struct PuzzleEntry {
        public string Name;
        public string[] Definition;
        public string BlockPath; // "Block1/Block2" or "" for top-level

        public PuzzleEntry(string name, string[] def, string blockPath) {
            Name = name;
            Definition = def;
            BlockPath = blockPath;
        }
    }

    static class PuzzleFileUtils {

        // ---- Add a puzzle to a specific block in the file ----
        public static bool AddPuzzle(string filePath, string puzzleName, string[] definition, string blockPath) {
            try {
                if (!File.Exists(filePath)) return false;
                var lines = new List<string>(File.ReadAllLines(filePath));

                // Remove trailing blank lines
                while (lines.Count > 0 && string.IsNullOrEmpty(lines[lines.Count - 1]))
                    lines.RemoveAt(lines.Count - 1);

                string[] blocks = ParseBlockPath(blockPath);
                int insertPos = -1;

                if (blocks.Length == 0) {
                    insertPos = lines.Count;
                } else {
                    insertPos = FindBlockEnd(lines, blocks);
                }

                if (insertPos < 0) {
                    // Exact path not found — try to find deepest existing ancestor
                    int start, end;
                    int parentDepth = FindDeepestBlock(lines, blocks, out start, out end);
                    if (parentDepth >= 0) {
                        // Parent found — insert missing nested blocks before its EndBlock
                        insertPos = end;
                        // Create missing suffix: blocks[parentDepth .. blocks.Length-1]
                        for (int i = parentDepth; i < blocks.Length; i++) {
                            lines.Insert(insertPos++, "");
                            lines.Insert(insertPos++, "Block " + blocks[i]);
                        }
                        // Remember position before adding EndBlocks (innermost block's content area)
                        int beforeEndBlocks = insertPos;
                        for (int i = parentDepth; i < blocks.Length; i++) {
                            lines.Insert(insertPos++, "EndBlock");
                        }
                        // Puzzle goes before the innermost EndBlock
                        insertPos = beforeEndBlocks;
                    } else {
                        // No ancestor exists — create entire path at end of file
                        insertPos = lines.Count;
                        lines.Add("");
                        for (int i = 0; i < blocks.Length; i++) {
                            lines.Add("Block " + blocks[i]);
                        }
                        lines.Add("");
                        for (int i = blocks.Length - 1; i >= 0; i--) {
                            lines.Add("EndBlock");
                        }
                        insertPos = lines.Count - blocks.Length;
                    }
                }

                List<string> newLines = new List<string>();
                newLines.Add("");
                newLines.Add("Puzzle " + puzzleName);
                foreach (string ln in definition) {
                    newLines.Add(ln);
                }

                lines.InsertRange(insertPos, newLines);

                File.WriteAllLines(filePath, lines.ToArray(), Encoding.UTF8);
                return true;
            } catch {
                return false;
            }
        }

        // ---- Remove a puzzle from a specific block, then clean up empty blocks ----
        public static bool RemovePuzzleFromBlock(string filePath, string puzzleName, string blockPath) {
            try {
                if (!File.Exists(filePath)) return false;
                var lines = new List<string>(File.ReadAllLines(filePath));

                int puzzleLine = FindPuzzleLineInBlock(lines, puzzleName, blockPath);
                if (puzzleLine < 0) return false;

                int endLine = FindNextSection(lines, puzzleLine + 1);
                if (endLine < 0) endLine = lines.Count;

                int startRemove = puzzleLine;
                if (startRemove > 0 && string.IsNullOrEmpty(lines[startRemove - 1]))
                    startRemove--;

                lines.RemoveRange(startRemove, endLine - startRemove);

                // Clean up empty parent blocks recursively
                CleanEmptyBlocks(lines, blockPath);

                File.WriteAllLines(filePath, lines.ToArray(), Encoding.UTF8);
                return true;
            } catch {
                return false;
            }
        }

        // After removing a puzzle, remove any ancestor blocks that became empty.
        static void CleanEmptyBlocks(List<string> lines, string blockPath) {
            string[] blocks = ParseBlockPath(blockPath);
            // Walk from innermost block outward
            for (int depth = blocks.Length; depth > 0; depth--) {
                string[] path = new string[depth];
                Array.Copy(blocks, path, depth);

                int blockStart, blockEnd;
                if (!FindBlockRange(lines, path, out blockStart, out blockEnd)) continue;

                // Check if block contains only blank/comment lines between Block and EndBlock
                bool empty = true;
                for (int i = blockStart + 1; i < blockEnd; i++) {
                    string t = lines[i].Trim();
                    if (t.Length > 0 && !t.StartsWith("#")) {
                        empty = false;
                        break;
                    }
                }
                if (!empty) break; // this block still has content, stop

                // Remove the block (including blank lines before Block)
                int start = blockStart;
                while (start > 0 && string.IsNullOrEmpty(lines[start - 1]))
                    start--;
                int end = blockEnd + 1;
                lines.RemoveRange(start, end - start);
            }
        }

        // ---- Get the original text of a puzzle from the file (global search) ----
        public static string GetPuzzleText(string filePath, string puzzleName) {
            try {
                if (!File.Exists(filePath)) return null;
                var lines = new List<string>(File.ReadAllLines(filePath));

                int puzzleLine = FindPuzzleLine(lines, puzzleName);
                if (puzzleLine < 0) return null;

                int endLine = FindNextSection(lines, puzzleLine + 1);
                if (endLine < 0) endLine = lines.Count;

                StringBuilder sb = new StringBuilder();
                for (int i = puzzleLine + 1; i < endLine; i++) {
                    if (sb.Length > 0) sb.AppendLine();
                    sb.Append(lines[i]);
                }
                return sb.ToString();
            } catch {
                return null;
            }
        }

        // ---- Check if a puzzle name already exists in the given block ----
        public static bool PuzzleExistsInBlock(Hashtable puzzleList, string puzzleName, string blockPath) {
            Hashtable target = NavigateToBlock(puzzleList, blockPath);
            if (target == null) return false;
            return target.ContainsKey(puzzleName);
        }

        // ---- Get a puzzle's definition from the in-memory Hashtable ----
        public static string[] GetPuzzleDefinition(Hashtable puzzleList, string puzzleName, string blockPath) {
            Hashtable target = NavigateToBlock(puzzleList, blockPath);
            if (target == null) return null;
            if (!target.ContainsKey(puzzleName)) return null;
            return target[puzzleName] as string[];
        }

        // ---- Flatten the puzzle list Hashtable into a list with block paths ----
        public static List<PuzzleEntry> GetAllPuzzles(Hashtable puzzleList, string parentPath = "") {
            var result = new List<PuzzleEntry>();
            foreach (DictionaryEntry de in puzzleList) {
                string key = (string)de.Key;
                if (de.Value is Hashtable) {
                    string childPath = string.IsNullOrEmpty(parentPath) ? key : parentPath + "/" + key;
                    result.AddRange(GetAllPuzzles((Hashtable)de.Value, childPath));
                } else if (de.Value is string[]) {
                    result.Add(new PuzzleEntry(key, (string[])de.Value, parentPath));
                }
            }
            return result;
        }

        // ---- Get all unique block paths from the puzzle list ----
        public static List<string> GetAllBlockPaths(Hashtable puzzleList, string parentPath = "") {
            var result = new List<string>();
            foreach (DictionaryEntry de in puzzleList) {
                string key = (string)de.Key;
                if (de.Value is Hashtable) {
                    string childPath = string.IsNullOrEmpty(parentPath) ? key : parentPath + "/" + key;
                    result.Add(childPath);
                    result.AddRange(GetAllBlockPaths((Hashtable)de.Value, childPath));
                }
            }
            return result;
        }

        // ======================== Helper methods ========================

        // Navigate the Hashtable tree to find the Hashtable for a given block path.
        static Hashtable NavigateToBlock(Hashtable root, string blockPath) {
            string[] blocks = ParseBlockPath(blockPath);
            if (blocks.Length == 0) return root;

            Hashtable current = root;
            foreach (string segment in blocks) {
                if (current.ContainsKey(segment) && current[segment] is Hashtable) {
                    current = (Hashtable)current[segment];
                } else {
                    return null;
                }
            }
            return current;
        }

        static string[] ParseBlockPath(string blockPath) {
            if (string.IsNullOrEmpty(blockPath) || blockPath.Trim().Length == 0)
                return new string[0];
            return blockPath.Split('/')
                .Select(b => b.Trim())
                .Where(b => b.Length > 0)
                .ToArray();
        }

        // Unified block locator: finds the "Block <name>" line (start) and its
        // matching "EndBlock" line (end) for the given path.
        // Only matches at the correct nesting level
        // (e.g. "CubeBased" only matches root-level, "4D/CubeBased" matches inside 4D).
        static bool FindBlockRange(List<string> lines, string[] blocks, out int start, out int end) {
            start = -1;
            end = -1;
            int blockIdx = 0;
            int matchDepth = 0;
            int fileDepth = 0;
            for (int i = 0; i < lines.Count; i++) {
                string trimmed = lines[i].Trim();
                if (trimmed.StartsWith("Block ")) {
                    string name = trimmed.Substring(6).Trim();
                    if (fileDepth == blockIdx && blockIdx < blocks.Length && name == blocks[blockIdx]) {
                        if (blockIdx == blocks.Length - 1) start = i;
                        matchDepth = 1;
                        blockIdx++;
                    } else if (matchDepth > 0) {
                        matchDepth++;
                    }
                    fileDepth++;
                } else if (trimmed == "EndBlock") {
                    fileDepth--;
                    if (matchDepth > 0) {
                        matchDepth--;
                        if (matchDepth == 0 && blockIdx == blocks.Length) {
                            end = i;
                            return start >= 0;
                        }
                    }
                }
            }
            return false;
        }

        static int FindBlockStart(List<string> lines, string[] blocks) {
            int start, end;
            if (FindBlockRange(lines, blocks, out start, out end)) return start;
            return -1;
        }

        static int FindBlockEnd(List<string> lines, string[] blocks) {
            int start, end;
            if (FindBlockRange(lines, blocks, out start, out end)) return end;
            return -1;
        }

        // Find the deepest existing ancestor block for a path.
        // Returns the number of path segments matched, and the range of that block.
        // For ["4D","4D"], if root "4D" exists but not nested "4D" inside,
        // returns parentDepth=1 with start/end of the root "4D" block.
        static int FindDeepestBlock(List<string> lines, string[] blocks, out int start, out int end) {
            for (int depth = blocks.Length - 1; depth > 0; depth--) {
                string[] path = new string[depth];
                Array.Copy(blocks, path, depth);
                if (FindBlockRange(lines, path, out start, out end)) return depth;
            }
            start = end = -1;
            return -1;
        }

        // Find a puzzle line within a specific block, or at top level (root).
        static int FindPuzzleLineInBlock(List<string> lines, string puzzleName, string blockPath) {
            string[] blocks = ParseBlockPath(blockPath);

            if (blocks.Length == 0) {
                // Root level: only match top-level puzzles (fileDepth == 0)
                int fileDepth = 0;
                for (int i = 0; i < lines.Count; i++) {
                    string trimmed = lines[i].Trim();
                    if (trimmed.StartsWith("Block ")) {
                        fileDepth++;
                    } else if (trimmed == "EndBlock") {
                        fileDepth--;
                    } else if (fileDepth == 0 && trimmed.StartsWith("Puzzle ")) {
                        string name = trimmed.Substring(7).Trim();
                        if (name == puzzleName) return i;
                    }
                }
                return -1;
            }

            int blockStart, blockEnd;
            if (!FindBlockRange(lines, blocks, out blockStart, out blockEnd)) return -1;

            // Search only at the direct level of this block (skip nested sub-blocks)
            int depth = 0;
            for (int i = blockStart + 1; i < blockEnd; i++) {
                string trimmed = lines[i].Trim();
                if (trimmed.StartsWith("Block ")) {
                    depth++;
                } else if (trimmed == "EndBlock") {
                    depth--;
                } else if (depth == 0 && trimmed.StartsWith("Puzzle ")) {
                    string name = trimmed.Substring(7).Trim();
                    if (name == puzzleName) return i;
                }
            }
            return -1;
        }

        // Find the line number of "Puzzle <name>" (global search)
        static int FindPuzzleLine(List<string> lines, string puzzleName) {
            for (int i = 0; i < lines.Count; i++) {
                string trimmed = lines[i].Trim();
                if (trimmed.StartsWith("Puzzle ")) {
                    string name = trimmed.Substring(7).Trim();
                    if (name == puzzleName) return i;
                }
            }
            return -1;
        }

        // Find the next section marker line after a given index
        static int FindNextSection(List<string> lines, int startIndex) {
            for (int i = startIndex; i < lines.Count; i++) {
                string trimmed = lines[i].Trim();
                if (trimmed.StartsWith("Puzzle ") ||
                    trimmed.StartsWith("Block ") ||
                    trimmed == "EndBlock") {
                    return i;
                }
            }
            return lines.Count;
        }
    }
}
