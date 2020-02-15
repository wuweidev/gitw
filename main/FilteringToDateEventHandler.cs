using System;

namespace gitw
{
    public delegate void FilteringToDateEventHandler(object sender, FilteringToDateEventArgs e);

    public class FilteringToDateEventArgs : EventArgs
    {
        public DateTimeOffset? ToDate { get; }

        public FilteringToDateEventArgs(DateTimeOffset? toDate)
        {
            this.ToDate = toDate;
        }
    }
}
