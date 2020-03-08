using System;
using System.ComponentModel;
using System.Drawing;
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

            this.OwnerDraw = true;
            this.Font = new Font(Constants.ListViewFontName, Constants.ListViewOwnerDrawFontSize);
            this.DrawColumnHeader += GitCommitListView_DrawColumnHeader;
            this.DrawItem += GitCommitListView_DrawItem;
            this.DrawSubItem += GitCommitListView_DrawSubItem;

            this.contextMenuItems = new ToolStripMenuItem[]
            {
                new ToolStripMenuItem("&Diff File", null, ContextMenu_DiffFile, Keys.Control | Keys.D),
                new ToolStripMenuItem("Diff &Entire Commit", null, ContextMenu_DiffCommit, Keys.Control | Keys.E),
                new ToolStripMenuItem("&View Log", null, ContextMenu_ViewLog, Keys.Control | Keys.O),
                new ToolStripMenuItem("&Copy", null, ContextMenu_Copy, Keys.Control | Keys.C),
                new ToolStripMenuItem("Copy &All", null, ContextMenu_CopyAll, Keys.Control | Keys.Shift | Keys.C),
            };

            this.ContextMenuStrip.Items.Clear();
            this.ContextMenuStrip.Items.AddRange(this.contextMenuItems);
            this.ContextMenuStrip.Opening += ContextMenuStrip_Opening;

            this.DoubleClick += Lv_DoubleClick;
            this.KeyDown += Lv_KeyDown;

            this.Disposed += Lv_Disposed;
        }

        public override int ActualListSize => this.owner.ListSize;

        private bool handlingWmPaint;

        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case Win32Native.WM_PAINT:
                    // When mouse is hovering over column 0, draw item/subitem
                    // events will be fired twice and cause flickering.
                    // Use this flag to avoid redrawing outside of WM_PAINT.
                    this.handlingWmPaint = true;
                    base.WndProc(ref m);
                    this.handlingWmPaint = false;
                    break;
                default:
                    base.WndProc(ref m);
                    break;
            }
        }

        private void GitCommitListView_DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            e.DrawDefault = true;
        }

        private void GitCommitListView_DrawItem(object sender, DrawListViewItemEventArgs e)
        {
            if (!this.handlingWmPaint) return;

            e.DrawBackground();

            // e.DrawFocusRectangle() leaves some space in the front which is not
            // consistent with default focus rectangle.
            if ((e.State & ListViewItemStates.Focused) != 0)
            {
                // Shrink a little bit to look similar to native focus rectangle.
                var bounds = Rectangle.Inflate(e.Item.Bounds, -1, -1);
                ControlPaint.DrawFocusRectangle(e.Graphics, bounds, e.Item.ForeColor, e.Item.BackColor);
            }
        }

        private void GitCommitListView_DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
            if (!this.handlingWmPaint) return;

            // Map the ColumnHeader.TextAlign to the TextFormatFlags.
            HorizontalAlignment hAlign = e.Header?.TextAlign ?? HorizontalAlignment.Left;
            TextFormatFlags flags =
                (hAlign == HorizontalAlignment.Left) ? TextFormatFlags.Left :
                (hAlign == HorizontalAlignment.Center) ? TextFormatFlags.HorizontalCenter :
                                                         TextFormatFlags.Right;
            flags |= TextFormatFlags.WordEllipsis | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding;

            string text = (e.ItemIndex == -1) ? e.Item.Text : e.SubItem.Text;
            Font normalFont = (e.ItemIndex == -1) ? e.Item.Font : e.SubItem.Font;
            Color normalColor = (e.ItemIndex == -1) ? e.Item.ForeColor : e.SubItem.ForeColor;
            // Add some padding before drawing text
            Rectangle newBounds = Rectangle.Inflate(e.Bounds, -6, 0);

            // Highlight hyperlinks
            Font linkFont = null;
            foreach (string s in text.ExtractHyperlinks())
            {
                if (string.IsNullOrEmpty(s)) continue;

                bool isHyperlink = s.StartsWith("http://") || s.StartsWith("https://") || s.StartsWith("www.");
                linkFont = (isHyperlink && linkFont == null) ? (new Font(normalFont, FontStyle.Underline)) : linkFont;
                var font = isHyperlink ? linkFont : normalFont;
                var color = isHyperlink ? Color.Blue : normalColor;
                TextRenderer.DrawText(e.Graphics, s, font, newBounds, color, flags);

                var size = new Size(newBounds.Width, newBounds.Height);
                var textSize = TextRenderer.MeasureText(e.Graphics, s, normalFont, size, flags);
                newBounds.X += textSize.Width;
                newBounds.Width -= textSize.Width;
            }
        }

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

        private void ContextMenu_CopyAll(object sender, EventArgs e)
        {
            this.ClipboardCopyAllItems();
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

        private void Lv_Disposed(object sender, EventArgs e)
        {
            this.owner.AwaitTask();
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
