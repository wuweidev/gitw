using System;
using System.Drawing;
using System.Windows.Forms;

namespace gitw
{
    public class GitListView : ListView
    {
        private bool contextMenuWithKeyboard;
        private readonly Timer updateTimer;
        private int headerHeight;

        public GitListView(IListViewOwner owner)
        {
            this.View = View.Details;
            this.Font = new Font(Constants.ListViewFontName, Constants.ListViewFontSize);
            this.FullRowSelect = true;
            this.MultiSelect = false;
            this.DoubleBuffered = true;
            this.ShowItemToolTips = true;
            this.VirtualMode = true;

            this.ContextMenuStrip = new ContextMenuStrip();
            this.ContextMenuStrip.Opened += ContextMenuStrip_Opened;

            this.VirtualListSize = owner != null ? owner.ListSize : throw new ArgumentNullException(nameof(owner));
            this.RetrieveVirtualItem += owner.Lv_RetrieveVirtualItem;
            this.CacheVirtualItems += owner.Lv_CacheVirtualItems;
            this.SearchForVirtualItem += owner.Lv_SearchForVirtualItem;

            this.HandleCreated += GitListView_HandleCreated;

            this.updateTimer = new Timer();
            this.updateTimer.Tick += UpdateTimer_Tick;
            this.updateTimer.Interval = Constants.ListViewTimerFirstInterval;
            this.updateTimer.Start();
        }

        public event EventHandler ListSizeChanged;

        public virtual int ActualListSize { get; }

        public void RefreshItems(bool redrawIfSizeUnchanged)
        {
            int oldSize = this.VirtualListSize;
            int newSize = this.ActualListSize;

            this.VirtualListSize = newSize;

            if (redrawIfSizeUnchanged && newSize == oldSize && newSize > 0)
            {
                RedrawItems(0, newSize - 1, true);
            }

            this.ListSizeChanged?.Invoke(this, null);
        }

        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case Win32Native.WM_CONTEXTMENU:
                    // -1 means context menu invoked with keyboard.
                    this.contextMenuWithKeyboard = m.LParam == IntPtr.Subtract(IntPtr.Zero, 1);
                    break;
            }

            base.WndProc(ref m);
        }

        private void ContextMenuStrip_Opened(object sender, EventArgs e)
        {
            if (this.contextMenuWithKeyboard && this.SelectedIndices.Count > 0)
            {
                var itemRect = this.GetItemRect(this.SelectedIndices[0]);
                if (itemRect.Bottom > this.headerHeight &&
                    itemRect.Top <= this.ClientRectangle.Bottom)
                {
                    var point = this.PointToScreen(itemRect.Location);
                    this.ContextMenuStrip.Top = point.Y + itemRect.Height;
                }
            }
        }

        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            this.updateTimer.Interval = Constants.ListViewTimerNormalInterval;

            RefreshItems(false);
        }

        private void GitListView_HandleCreated(object sender, EventArgs e)
        {
            this.SetExplorerTheme();

            this.headerHeight = this.GetHeaderHeight();
        }
    }
}
