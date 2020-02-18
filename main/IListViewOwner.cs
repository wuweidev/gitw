using System;
using System.Windows.Forms;

namespace gitw
{
    public interface IListViewOwner
    {
        int ListSize { get; }

        // Invoked before and after an async task is executed.
        event EventHandler TaskBegin;
        event EventHandler TaskEnd;

        void InitializeTask();

        void Lv_SearchForVirtualItem(object sender, SearchForVirtualItemEventArgs e);
        void Lv_CacheVirtualItems(object sender, CacheVirtualItemsEventArgs e);
        void Lv_RetrieveVirtualItem(object sender, RetrieveVirtualItemEventArgs e);
    }
}
