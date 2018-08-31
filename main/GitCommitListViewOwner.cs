using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using LibGit2Sharp;

namespace gitw
{
    public class GitCommitListViewOwner : CacheListViewOwner
    {
        private GitLog gitLog;
        private readonly Commit commit;
        private readonly List<string> baseFileRelPaths;
        private readonly List<string> nonBaseFileRelPaths;
        private readonly List<string> headerRows;
        private readonly List<string> baseFileRows;
        private readonly List<string> nonBaseFileRows;

        public GitCommitListViewOwner(GitLog gitLog, Commit commit)
        {
            this.gitLog = gitLog ?? throw new ArgumentNullException(nameof(gitLog));
            this.commit = commit ?? throw new ArgumentNullException(nameof(commit));

            this.baseFileRelPaths = new List<string>();
            this.nonBaseFileRelPaths = new List<string>();
            this.headerRows = new List<string>();
            this.baseFileRows = new List<string>();
            this.nonBaseFileRows = new List<string>();

            Task.Factory.StartNew(() => GenerateCommitContent());
        }

        public override int ListSize => this.headerRows.Count + this.baseFileRows.Count + this.nonBaseFileRows.Count;

        public bool ShowDiffForSelection(int selectedIndex)
        {
            string selectedPath = GetSelectedPath(selectedIndex);
            if (selectedPath == null) return false;

            this.gitLog.ShowDiff(this.commit, selectedPath);
            return true;
        }

        public void ShowDiffForCommit()
        {
            this.gitLog.ShowDiff(this.commit, forEntireCommit: true);
        }

        public string GetSelectedPath(int index)
        {
            if (this.headerRows.Count < index && index < this.headerRows.Count + this.baseFileRows.Count)
            {
                return this.baseFileRelPaths[index - this.headerRows.Count];
            }
            else if (this.headerRows.Count + this.baseFileRows.Count < index && index < this.headerRows.Count + this.baseFileRows.Count + this.nonBaseFileRows.Count)
            {
                return this.nonBaseFileRelPaths[index - this.headerRows.Count - this.baseFileRows.Count];
            }
            else
            {
                return null;
            }
        }

        public bool ShowFileLog(int selectedIndex)
        {
            string selectedPath = GetSelectedPath(selectedIndex);
            if (selectedPath == null) return false;

            string fullPath = Path.Combine(this.gitLog.Repo.Info.WorkingDirectory, selectedPath);
            var form = new GitLogForm(this.gitLog.Repo, fullPath);
            Program.AppContext.NewForm(form);
            return true;
        }

        protected override ListViewItem CreateVirtualItem(int index)
        {
            if (0 <= index && index < this.headerRows.Count)
            {
                return new ListViewItem(this.headerRows[index]);
            }
            else if (this.headerRows.Count <= index && index < this.headerRows.Count + this.baseFileRows.Count)
            {
                return new ListViewItem(this.baseFileRows[index - this.headerRows.Count]);
            }
            else if (this.headerRows.Count + this.baseFileRows.Count <= index && index < this.headerRows.Count + this.baseFileRows.Count + this.nonBaseFileRows.Count)
            {
                return new ListViewItem(this.nonBaseFileRows[index - this.headerRows.Count - this.baseFileRows.Count]);
            }
            else
            {
                return null;
            }
        }

        private void GenerateCommitContent()
        {
            this.headerRows.Add($"Change: {this.commit.Sha}");

            if (this.commit.Parents != null)
            {
                foreach (var parent in this.commit.Parents)
                {
                    this.headerRows.Add($"Parent: {parent.Sha}");
                }
            }

            this.headerRows.AddRange(new[]
            {
                $"Author: {this.commit.Author}  {this.commit.Author.When.ToLocalTimeString()}",
                $"Committer: {this.commit.Committer}  {this.commit.Committer.When.ToLocalTimeString()}",
                string.Empty,
            });

            this.headerRows.AddRange(
                Commit.PrettifyMessage(this.commit.Message, '#').Split('\n'));

            this.headerRows.Add(string.Empty);
            this.headerRows.Add("Affected files ...");

            var patch = this.gitLog.GetCommitPatch(this.commit);

            foreach (var pe in patch)
            {
                if (pe.Path.StartsWith(this.gitLog.TargetPath, StringComparison.OrdinalIgnoreCase) ||
                    pe.OldPath.StartsWith(this.gitLog.TargetPath, StringComparison.OrdinalIgnoreCase))
                {
                    if (this.baseFileRows.Count == 0)
                    {
                        this.baseFileRelPaths.Add(string.Empty);
                        this.baseFileRows.Add(string.Empty);
                    }
                    this.baseFileRelPaths.Add(pe.Path);
                    this.baseFileRows.Add($"{pe.Path} ({pe.Status})");
                }
                else
                {
                    if (this.nonBaseFileRows.Count == 0)
                    {
                        this.nonBaseFileRelPaths.Add(string.Empty);
                        this.nonBaseFileRows.Add(string.Empty);
                    }
                    this.nonBaseFileRelPaths.Add(pe.Path);
                    this.nonBaseFileRows.Add($"{pe.Path} ({pe.Status})");
                }
            }
        }
    }
}
