using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using LibGit2Sharp;

namespace gitw
{
    public class GitLogListViewOwner : CacheListViewOwner
    {
        private GitLog gitLog;
        private IList<Commit> commits;              // ListViewItem for commits are cached by CachedListView; no need to keep ListViewItem here.
                                                    // Also need original commit for filtering.
        private IList<ListViewItem> matchingItems;  // Filtered commits are uncached, so keep ListViewItem directly.
        private string filterText;
        private Signature filterAuthor;
        private DateTimeOffset? filterFromDate;
        private DateTimeOffset? filterToDate;
        private CancellationTokenSource tokenSource;
        private Task task;

        public GitLogListViewOwner(GitLog gitLog)
        {
            this.gitLog = gitLog ?? throw new ArgumentNullException(nameof(gitLog));
            this.commits = new List<Commit>();
            this.tokenSource = new CancellationTokenSource();
            this.task = Task.Factory.StartNew(
                () => gitLog.Commits.AddToList(this.commits, this.tokenSource.Token),
                this.tokenSource.Token);
        }

        public override int ListSize => this.matchingItems == null ? this.commits.Count : this.matchingItems.Count;

        public bool TargetIsDirectory => this.gitLog.TargetIsDirectory;

        public bool FilterByText(string text)
        {
            if (text == this.filterText) return false;

            this.filterText = text;

            if (FilterIsEmpty())
            {
                var items = this.matchingItems;
                this.matchingItems = null;

                items?.Clear();

                this.EnableCache = true;
            }
            else
            {
                this.matchingItems = new List<ListViewItem>();

                //TODO: cancel old task?
                Task.Factory.StartNew(() =>
                {
                    this.commits.FilterToListViewItems(this.filterText, this.filterAuthor, this.filterFromDate, this.filterToDate, this.matchingItems);
                });

                this.EnableCache = false;
            }

            return true;
        }

        public bool FilterByAuthor(object itemTag, out string authorName)
        {
            authorName = null;
            var commit = itemTag as Commit;
            if (commit == null) return false;

            var author = commit.Author;
            authorName = author.ToString();

            if (this.filterAuthor != null &&
                string.Equals(this.filterAuthor.Email, author.Email, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            this.filterAuthor = author;

            this.matchingItems = new List<ListViewItem>();

            Task.Factory.StartNew(() =>
            {
                this.commits.FilterToListViewItems(this.filterText, this.filterAuthor, this.filterFromDate, this.filterToDate, this.matchingItems);
            });

            this.EnableCache = false;

            return true;
        }

        public bool FilterFromDate(object itemTag, out DateTimeOffset? fromDate)
        {
            fromDate = null;
            var commit = itemTag as Commit;
            if (commit == null) return false;

            fromDate = commit.Author.When;

            if (this.filterFromDate.HasValue && this.filterFromDate.Value == fromDate)
            {
                return false;
            }

            this.filterFromDate = fromDate;

            this.matchingItems = new List<ListViewItem>();

            Task.Factory.StartNew(() =>
            {
                this.commits.FilterToListViewItems(this.filterText, this.filterAuthor, this.filterFromDate, this.filterToDate, this.matchingItems);
            });

            this.EnableCache = false;

            return true;
        }

        public bool FilterToDate(object itemTag, out DateTimeOffset? toDate)
        {
            toDate = null;
            var commit = itemTag as Commit;
            if (commit == null) return false;

            toDate = commit.Author.When;

            if (this.filterToDate.HasValue && this.filterToDate.Value == toDate)
            {
                return false;
            }

            this.filterToDate = toDate;

            this.matchingItems = new List<ListViewItem>();

            Task.Factory.StartNew(() =>
            {
                this.commits.FilterToListViewItems(this.filterText, this.filterAuthor, this.filterFromDate, this.filterToDate, this.matchingItems);
            });

            this.EnableCache = false;

            return true;
        }

        public bool ClearFiltering()
        {
            if (this.matchingItems == null) return false;

            var items = this.matchingItems;
            this.matchingItems = null;

            items?.Clear();

            this.EnableCache = true;

            this.filterText = string.Empty;
            this.filterAuthor = null;
            this.filterFromDate = null;
            this.filterToDate = null;

            return true;
        }

        public bool SelectBranch(string branchName)
        {
            if (!this.gitLog.SelectBranch(branchName)) return false;

            var oldTask = this.task;
            var oldTokenSource = this.tokenSource;
            var oldCommits = this.commits;
            var oldMatchingItems = this.matchingItems;

            this.commits = new List<Commit>();
            this.matchingItems = FilterIsEmpty() ?  null : new List<ListViewItem>();
            this.tokenSource = new CancellationTokenSource();
            this.task = Task.Factory.StartNew(
                () =>
                {
                    oldTokenSource.Cancel();
                    oldTask.Wait();
                    oldTokenSource.Dispose();
                    oldCommits.Clear();
                    oldMatchingItems?.Clear();

                    this.gitLog.Commits.AddToList(this.commits, this.tokenSource.Token);
                    this.commits.FilterToListViewItems(this.filterText, this.filterAuthor, this.filterFromDate, this.filterToDate, this.matchingItems);
                },
                this.tokenSource.Token);

            return true;
        }

        public void ShowDiff(object itemTag, bool forEntireCommit)
        {
            var commit = itemTag as Commit;
            if (commit == null) return;

            this.gitLog.ShowDiff(commit, forEntireCommit: forEntireCommit);
        }

        public void ShowDiffWithHead(object itemTag)
        {
            var commit = itemTag as Commit;
            if (commit == null) return;

            this.gitLog.ShowDiffWithHead(commit);
        }

        public void ShowCommitContent(object itemTag)
        {
            var commit = itemTag as Commit;
            if (commit == null) return;

            var form = new GitCommitForm(this.gitLog, commit);
            Program.AppContext.NewForm(form);
        }

        public void CancelTask()
        {
            this.tokenSource.Cancel();
            this.task.Wait();
            this.tokenSource.Dispose();
        }

        protected override ListViewItem CreateVirtualItem(int index)
        {
            if (this.matchingItems == null)
            {
                return 0 <= index && index < this.commits.Count ?
                    this.commits[index].ToListViewItem() :
                    null;
            }
            else
            {
                return 0 <= index && index < this.matchingItems.Count ?
                    this.matchingItems[index] :
                    null;
            }
        }

        private bool FilterIsEmpty()
        {
            return string.IsNullOrEmpty(this.filterText) && this.filterAuthor == null && !this.filterFromDate.HasValue && !this.filterToDate.HasValue;
        }
    }
}
