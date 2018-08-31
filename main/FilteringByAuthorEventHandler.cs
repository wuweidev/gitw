using System;

namespace gitw
{
    public delegate void FilteringByAuthorEventHandler(object sender, FilteringByAuthorEventArgs e);

    public class FilteringByAuthorEventArgs : EventArgs
    {
        public string AuthorName { get; }

        public FilteringByAuthorEventArgs(string authorName)
        {
            this.AuthorName = authorName;
        }
    }
}
