using System;

namespace gitw
{
    public delegate void FilteringFromDateEventHandler(object sender, FilteringFromDateEventArgs e);

    public class FilteringFromDateEventArgs : EventArgs
    {
        public DateTimeOffset? FromDate { get; }

        public FilteringFromDateEventArgs(DateTimeOffset? fromDate)
        {
            this.FromDate = fromDate;
        }
    }
}
