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
                new ToolStripMenuItem("Diff with &Head", null, ContextMenu_DiffCommitWithHead, Keys.Control | Keys.H),
                new ToolStripMenuItem("Diff &Entire Commit", null, ContextMenu_DiffEntireCommit, Keys.Control | Keys.E),
                new ToolStripMenuItem("&Diff Commit", null, ContextMenu_DiffCommit, Keys.Control | Keys.D),
                new ToolStripMenuItem("&View Commit", null, ContextMenu_ViewCommit, Keys.Control | Keys.O),
                new ToolStripMenuItem("&Copy", null, ContextMenu_Copy, Keys.Control | Keys.C),
                new ToolStripMenuItem("Copy Commit &ID", null, ContextMenu_CopyID, Keys.None),
                new ToolStripMenuItem("Filter by &Author", null, ContextMenu_FilterByAuthor, Keys.None),
                new ToolStripMenuItem("Filter &from Date", null, ContextMenu_FilterFromDate, Keys.None),
                new ToolStripMenuItem("Filter &to Date", null, ContextMenu_FilterToDate, Keys.None),
                new ToolStripMenuItem("AutoFit Column &Width", null, ContextMenu_AutoFitColumnWidth, Keys.None),
            };

            this.ContextMenuStrip.Items.Clear();
            this.ContextMenuStrip.Items.AddRange(this.contextMenuItems);
            this.ContextMenuStrip.Opening += ContextMenuStrip_Opening;

            this.DoubleClick += Lv_DoubleClick;
            this.KeyDown += Lv_KeyDown;

            this.Disposed += Lv_Disposed;
        }

        public event FilteringByAuthorEventHandler FilteringByAuthor;
        public event FilteringFromDateEventHandler FilteringFromDate;
        public event FilteringToDateEventHandler FilteringToDate;

        public override int ActualListSize => this.owner.ListSize;

        public void FilterBySearchText(string text)
        {
            if (this.owner.FilterByText(text))
            {
                RefreshItems(true);
            }
        }

        public void CancelFilterByAuthor()
        {
            if (this.owner.CancelFilterByAuthor())
            {
                RefreshItems(true);

                this.FilteringByAuthor?.Invoke(this, new FilteringByAuthorEventArgs(null));
            }
        }

        public void ClearFiltering()
        {
            if (this.owner.ClearFiltering())
            {
                RefreshItems(true);

                this.FilteringByAuthor?.Invoke(this, new FilteringByAuthorEventArgs(string.Empty));
                this.FilteringFromDate?.Invoke(this, new FilteringFromDateEventArgs(null));
                this.FilteringToDate?.Invoke(this, new FilteringToDateEventArgs(null));
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
            this.contextMenuItems[1].Enabled =
            this.contextMenuItems[2].Enabled = !this.owner.TargetIsDirectory;
            this.contextMenuItems[3].Enabled = this.owner.TargetIsDirectory;
            this.contextMenuItems[4].Enabled =
            this.contextMenuItems[5].Enabled =
            this.contextMenuItems[6].Enabled = true;

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
            ShowDiffForSelection(forEntireCommit: true);
        }

        private void ContextMenu_DiffCommitWithHead(object sender, EventArgs e)
        {
            ShowDiffWithHead();
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

        private void ContextMenu_FilterFromDate(object sender, EventArgs e)
        {
            if (this.SelectedIndices.Count == 0) return;

            var item = this.Items[this.SelectedIndices[0]];
            if (this.owner.FilterFromDate(item.Tag, out DateTimeOffset? fromDate))
            {
                RefreshItems(true);

                this.FilteringFromDate?.Invoke(this, new FilteringFromDateEventArgs(fromDate));
            }
        }

        private void ContextMenu_FilterToDate(object sender, EventArgs e)
        {
            if (this.SelectedIndices.Count == 0) return;

            var item = this.Items[this.SelectedIndices[0]];
            if (this.owner.FilterToDate(item.Tag, out DateTimeOffset? toDate))
            {
                RefreshItems(true);

                this.FilteringToDate?.Invoke(this, new FilteringToDateEventArgs(toDate));
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

        private void Lv_Disposed(object sender, EventArgs e)
        {
            this.owner.CancelTask();
        }

        private bool ShowDiffForSelection(bool forEntireCommit = false)
        {
            if (this.SelectedIndices.Count == 0) return false;

            var item = this.Items[this.SelectedIndices[0]];
            this.owner.ShowDiff(item.Tag, forEntireCommit);
            return true;
        }

        private bool ShowDiffWithHead()
        {
            if (this.SelectedIndices.Count == 0) return false;

            var item = this.Items[this.SelectedIndices[0]];
            this.owner.ShowDiffWithHead(item.Tag);
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
