using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace gitw
{
    public class GitLogListView : GitListView
    {
        private readonly GitLogListViewOwner owner;
        private readonly ToolStripMenuItem[] contextMenuItems;

        public GitLogListView(GitLogListViewOwner owner) : base(owner)
        {
            this.owner = owner;

            this.Columns.Clear();
            this.Columns.AddRange(new ColumnHeader[]
            {
                new ColumnHeader() { Text = "Change", Width = 100, },
                new ColumnHeader() { Text = "Date", Width = 150, },
                new ColumnHeader() { Text = "Author", Width = 250, },
                new ColumnHeader() { Text = "Comment", Width = 650, },
            });

            this.HeaderStyle = ColumnHeaderStyle.Nonclickable;

            this.contextMenuItems = new ToolStripMenuItem[]
            {
                new ToolStripMenuItem("&Diff File", null, ContextMenu_DiffCommit, Keys.Control | Keys.D),
                new ToolStripMenuItem("Diff &Entire Commit", null, ContextMenu_DiffEntireCommit, Keys.Control | Keys.E),
                new ToolStripMenuItem("&Diff Commit", null, ContextMenu_DiffCommit, Keys.Control | Keys.D),
                new ToolStripMenuItem("&View Commit", null, ContextMenu_ViewCommit, Keys.Control | Keys.O),
                new ToolStripMenuItem("&Copy", null, ContextMenu_Copy, Keys.Control | Keys.C),
                new ToolStripMenuItem("Copy Commit &ID", null, ContextMenu_CopyID, Keys.None),
                new ToolStripMenuItem("Filter by &Author", null, ContextMenu_FilterByAuthor, Keys.None),
                new ToolStripMenuItem("AutoFit Column &Width", null, ContextMenu_AutoFitColumnWidth, Keys.None),
            };

            this.ContextMenuStrip.Items.Clear();
            this.ContextMenuStrip.Items.AddRange(this.contextMenuItems);
            this.ContextMenuStrip.Opening += ContextMenuStrip_Opening;

            this.DoubleClick += Lv_DoubleClick;
            this.KeyDown += Lv_KeyDown;
        }

        public event FilteringByAuthorEventHandler FilteringByAuthor;

        public override int ActualListSize => this.owner.ListSize;

        public void FilterBySearchText(string text)
        {
            if (this.owner.FilterByText(text))
            {
                RefreshItems(true);
            }
        }

        public void ClearFiltering()
        {
            if (this.owner.ClearFiltering())
            {
                RefreshItems(true);

                this.FilteringByAuthor?.Invoke(this, new FilteringByAuthorEventArgs(string.Empty));
            }
        }

        public void SelectBranch(string branchName)
        {
            if (this.owner.SelectBranch(branchName))
            {
                RefreshItems(true);
            }
        }

        private void ContextMenuStrip_Opening(object sender, CancelEventArgs e)
        {
            if (this.SelectedIndices.Count <= 0)
            {
                e.Cancel = true;
                return;
            }

            this.contextMenuItems[0].Enabled =
            this.contextMenuItems[1].Enabled = !this.owner.TargetIsDirectory;
            this.contextMenuItems[2].Enabled = this.owner.TargetIsDirectory;
            this.contextMenuItems[3].Enabled =
            this.contextMenuItems[4].Enabled =
            this.contextMenuItems[5].Enabled = true;

            foreach (var item in this.contextMenuItems)
            {
                item.Visible = item.Enabled;
            }
        }

        private void ContextMenu_DiffCommit(object sender, EventArgs e)
        {
            ShowDiffForSelection();
        }

        private void ContextMenu_DiffEntireCommit(object sender, EventArgs e)
        {
            ShowDiffForSelection();
        }

        private void ContextMenu_ViewCommit(object sender, EventArgs e)
        {
            ShowCommitContent();
        }

        private void ContextMenu_Copy(object sender, EventArgs e)
        {
            this.ClipboardCopyItem();
        }

        private void ContextMenu_CopyID(object sender, EventArgs e)
        {
            if (this.SelectedIndices.Count == 0) return;

            var item = this.Items[this.SelectedIndices[0]];
            item.SubItems[0].Text.CopyToClipboard();
        }

        private void ContextMenu_AutoFitColumnWidth(object sender, EventArgs e)
        {
            this.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
        }

        private void ContextMenu_FilterByAuthor(object sender, EventArgs e)
        {
            if (this.SelectedIndices.Count == 0) return;

            var item = this.Items[this.SelectedIndices[0]];
            if (this.owner.FilterByAuthor(item.Tag, out string authorName))
            {
                RefreshItems(true);

                this.FilteringByAuthor?.Invoke(this, new FilteringByAuthorEventArgs(authorName));
            }
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

            var item = this.Items[this.SelectedIndices[0]];
            this.owner.ShowDiff(item.Tag);
            return true;
        }

        private bool ShowCommitContent()
        {
            if (this.SelectedIndices.Count == 0) return false;

            var item = this.Items[this.SelectedIndices[0]];
            this.owner.ShowCommitContent(item.Tag);
            return true;
        }
    }
}
