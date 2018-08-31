using System.Windows.Forms;

namespace gitw
{
    public interface IListViewOwner
    {
        int ListSize { get; }

        void Lv_SearchForVirtualItem(object sender, SearchForVirtualItemEventArgs e);
        void Lv_CacheVirtualItems(object sender, CacheVirtualItemsEventArgs e);
        void Lv_RetrieveVirtualItem(object sender, RetrieveVirtualItemEventArgs e);
    }
}
