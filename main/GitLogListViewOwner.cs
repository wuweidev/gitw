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
        public override event EventHandler TaskBegin;
        public override event EventHandler TaskEnd;

        private GitLog gitLog;
        private IList<Commit> commits;  // ListViewItem for commits are cached by CachedListView; no need to keep ListViewItem here.
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
        }

        public override int ListSize => this.matchingItems == null ? this.commits.Count : this.matchingItems.Count;

        public bool TargetIsDirectory => this.gitLog.TargetIsDirectory;

        public override void InitializeTask()
        {
            this.tokenSource = new CancellationTokenSource();
            this.task = Task.Factory.StartNew(
                () =>
                {
                    this.TaskBegin.Invoke(this, null);
                    this.gitLog.Commits.AddToList(this.commits, this.tokenSource.Token);
                    this.TaskEnd.Invoke(this, null);
                },
                this.tokenSource.Token);
        }

        public bool FilterByText(string text)
        {
            if (text == this.filterText) return false;

            this.filterText = text;

            ResetFiltering();

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

            ResetFiltering();

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

            ResetFiltering();

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

            ResetFiltering();

            return true;
        }

        public bool CancelFilterByAuthor()
        {
            if (this.filterAuthor == null) return false;

            this.filterAuthor = null;

            ResetFiltering();

            return true;
        }

        public bool CancelFilterFromDate()
        {
            if (this.filterFromDate == null) return false;

            this.filterFromDate = null;

            ResetFiltering();

            return true;
        }

        public bool CancelFilterToDate()
        {
            if (this.filterToDate == null) return false;

            this.filterToDate = null;

            ResetFiltering();

            return true;
        }

        public bool ClearFiltering()
        {
            if (this.matchingItems == null) return false;

            this.filterText = string.Empty;
            this.filterAuthor = null;
            this.filterFromDate = null;
            this.filterToDate = null;

            ResetFiltering();

            return true;
        }

        public bool SelectBranch(int branchIndex)
        {
            if (!this.gitLog.SelectBranch(branchIndex)) return false;

            CancelAndStartNewFilteringTask(true);

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

        private void ResetFiltering()
        {
            if (FilterIsEmpty())
            {
                var items = this.matchingItems;
                this.matchingItems = null;

                items?.Clear();

                this.EnableCache = true;
            }
            else
            {
                CancelAndStartNewFilteringTask(false);

                this.EnableCache = false;
            }
        }

        private bool FilterIsEmpty()
        {
            return string.IsNullOrEmpty(this.filterText) && this.filterAuthor == null && !this.filterFromDate.HasValue && !this.filterToDate.HasValue;
        }

        private void CancelAndStartNewFilteringTask(bool enumerating)
        {
            var oldTask = this.task;
            var oldTokenSource = this.tokenSource;
            var oldCommits = this.commits;
            var oldMatchingItems = this.matchingItems;

            if (enumerating)
            {
                this.commits = new List<Commit>();
            }
            this.matchingItems = FilterIsEmpty() ? null : new List<ListViewItem>();
            this.tokenSource = new CancellationTokenSource();
            this.task = Task.Factory.StartNew(
                () =>
                {
                    oldTokenSource.Cancel();
                    oldTask.Wait();
                    oldTokenSource.Dispose();
                    if (enumerating)
                    {
                        oldCommits?.Clear();
                    }
                    oldMatchingItems?.Clear();

                    this.TaskBegin.Invoke(this, null);
                    if (enumerating)
                    {
                        this.gitLog.Commits.AddToList(this.commits, this.tokenSource.Token);
                    }
                    this.commits.FilterToListViewItems(
                        this.filterText,
                        this.filterAuthor,
                        this.filterFromDate,
                        this.filterToDate,
                        this.matchingItems,
                        this.tokenSource.Token);
                    this.TaskEnd.Invoke(this, null);
                },
                this.tokenSource.Token);
        }
    }
}
