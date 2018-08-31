using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace gitw
{
    public class GitCommitListView : GitListView
    {
        private readonly GitCommitListViewOwner owner;
        private readonly ToolStripMenuItem[] contextMenuItems;

        public GitCommitListView(GitCommitListViewOwner owner) : base(owner)
        {
            this.owner = owner;

            this.Columns.Clear();
            this.Columns.AddRange(new ColumnHeader[]
            {
                new ColumnHeader() { Text = string.Empty, Width = 1150, },
            });

            this.HeaderStyle = ColumnHeaderStyle.None;

            this.contextMenuItems = new ToolStripMenuItem[]
            {
                new ToolStripMenuItem("&Diff File", null, ContextMenu_DiffFile, Keys.Control | Keys.D),
                new ToolStripMenuItem("Diff &Entire Commit", null, ContextMenu_DiffCommit, Keys.Control | Keys.E),
                new ToolStripMenuItem("&View Log", null, ContextMenu_ViewLog, Keys.Control | Keys.O),
                new ToolStripMenuItem("&Copy", null, ContextMenu_Copy, Keys.Control | Keys.C),
            };

            this.ContextMenuStrip.Items.Clear();
            this.ContextMenuStrip.Items.AddRange(this.contextMenuItems);
            this.ContextMenuStrip.Opening += ContextMenuStrip_Opening;

            this.DoubleClick += Lv_DoubleClick;
            this.KeyDown += Lv_KeyDown;
        }

        public override int ActualListSize => this.owner.ListSize;

        private void ContextMenuStrip_Opening(object sender, CancelEventArgs e)
        {
            string selectedPath = this.SelectedIndices.Count > 0 ?
                this.owner.GetSelectedPath(this.SelectedIndices[0]) : null;

            // Only show diff menu for file rows
            this.contextMenuItems[0].Enabled =
            this.contextMenuItems[2].Enabled = selectedPath != null;
            this.contextMenuItems[3].Enabled = this.SelectedIndices.Count > 0;

            foreach (var item in this.contextMenuItems)
            {
                item.Visible = item.Enabled;
            }
        }

        private void ContextMenu_Copy(object sender, EventArgs e)
        {
            this.ClipboardCopyItem();
        }

        private void ContextMenu_DiffFile(object sender, EventArgs e)
        {
            ShowDiffForSelection();
        }

        private void ContextMenu_DiffCommit(object sender, EventArgs e)
        {
            ShowDiffForCommit();
        }

        private void ContextMenu_ViewLog(object sender, EventArgs e)
        {
            ShowFileLog();
        }

        private void Lv_DoubleClick(object sender, EventArgs e)
        {
            ShowDiffForSelection();
        }

        private void Lv_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Handled) return;

            if (e.KeyData == Keys.Return)
            {
                e.Handled = ShowDiffForSelection();
            }
            else if (e.KeyData == (Keys.Control | Keys.Insert))
            {
                e.Handled = this.ClipboardCopyItem();
            }
        }

        private bool ShowDiffForSelection()
        {
            if (this.SelectedIndices.Count == 0) return false;

            return this.owner.ShowDiffForSelection(this.SelectedIndices[0]);
        }

        private void ShowDiffForCommit()
        {
            this.owner.ShowDiffForCommit();
        }

        private bool ShowFileLog()
        {
            if (this.SelectedIndices.Count == 0) return false;

            return this.owner.ShowFileLog(this.SelectedIndices[0]);
        }
    }
}
