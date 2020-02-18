using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace gitw
{
    public abstract class CacheListViewOwner : IListViewOwner
    {
        protected IDictionary<int, WeakReference> itemCache;

        protected CacheListViewOwner()
        {
            this.itemCache = new Dictionary<int, WeakReference>();
            this.EnableCache = true;
        }

        public abstract int ListSize { get; }

        public abstract event EventHandler TaskBegin;
        public abstract event EventHandler TaskEnd;

        protected virtual bool EnableCache { get; set; }

        public abstract void InitializeTask();

        public virtual void Lv_CacheVirtualItems(object sender, CacheVirtualItemsEventArgs e)
        {
            if (!this.EnableCache) return;

            for (int i = e.StartIndex; i <= e.EndIndex; ++i)
            {
                if (this.itemCache.TryGetValue(i, out WeakReference weak))
                {
                    if (!weak.IsAlive)
                    {
                        weak.Target = CreateVirtualItem(i);
                    }
                }
                else
                {
                    var item = CreateVirtualItem(i);
                    if (item != null)
                    {
                        weak = new WeakReference(item);
                        this.itemCache.Add(i, weak);
                    }
                }
            }
        }

        public virtual void Lv_RetrieveVirtualItem(object sender, RetrieveVirtualItemEventArgs e)
        {
            if (!this.EnableCache)
            {
                e.Item = CreateVirtualItem(e.ItemIndex);
                return;
            }

            if (this.itemCache.TryGetValue(e.ItemIndex, out WeakReference weak))
            {
                e.Item = weak.Target as ListViewItem;
                if (e.Item == null)
                {
                    e.Item = CreateVirtualItem(e.ItemIndex);
                    weak.Target = e.Item;
                }
            }
            else
            {
                e.Item = CreateVirtualItem(e.ItemIndex);
                if (e.Item != null)
                {
                    weak = new WeakReference(e.Item);
                    this.itemCache.Add(e.ItemIndex, weak);
                }
            }
        }

        public virtual void Lv_SearchForVirtualItem(object sender, SearchForVirtualItemEventArgs e)
        {
        }

        protected abstract ListViewItem CreateVirtualItem(int index);
    }
}
