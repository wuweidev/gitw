using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using LibGit2Sharp;

namespace gitw
{
    public class GitLog
    {
        private static readonly char[] PathSeparators = new char[] { '\\' };

        private Repository repo;
        private string targetPath;
        private readonly int count;
        private List<Branch> branches;
        private int currentBranchIndex;
        private bool headDeteched;
        private string diffCmd;
        private string diffExe;
        private string diffArguments;
        private IEnumerable<Commit> cachedCommits;
#if USE_COMMIT_MAP
        private IDictionary<Commit, TreeEntry> map;
#endif

        public GitLog(Repository repo, string path, int count)
        {
            this.repo = repo ?? throw new ArgumentNullException(nameof(repo));
            this.targetPath = path ?? throw new ArgumentNullException(nameof(path));
            this.count = count > 0 ? count : throw new ArgumentOutOfRangeException(nameof(count));

            this.branches = repo.Branches.Where(b => !b.IsRemote).ToList();
            this.headDeteched = true;
            for (int i = 0; i < this.branches.Count; ++i)
            {
                if (this.branches[i].IsCurrentRepositoryHead)
                {
                    this.currentBranchIndex = i;
                    this.headDeteched = false;
                    break;
                }
            }
            if (this.headDeteched)
            {
                this.branches.Insert(0, repo.Head);
                this.currentBranchIndex = 0;
            }

            Debug.Assert(this.targetPath.StartsWith(this.repo.Info.WorkingDirectory, StringComparison.OrdinalIgnoreCase));
            this.targetPath = this.targetPath.Substring(this.repo.Info.WorkingDirectory.Length);

            ParseDiffTool();

#if USE_COMMIT_MAP
            this.map = new Dictionary<Commit, TreeEntry>();
#endif
        }

        public IEnumerable<Commit> Commits
        {
            get
            {
                if (this.cachedCommits == null)
                {
                    this.cachedCommits = new CachedEnumerable<Commit>(FetchCommits(this.count));
                }

                return this.cachedCommits;
            }
        }

        public Repository Repo => this.repo;

        public string TargetPath => this.targetPath;

        public bool TargetIsDirectory => this.targetPath.Length == 0 || this.targetPath.EndsWith("\\", StringComparison.Ordinal);

        public string[] GetLocalBranchNames(out int currentBranchIndex)
        {
            currentBranchIndex = this.currentBranchIndex;

            var branchNames = new List<string>();
            for (int i = 0; i < this.branches.Count; ++i)
            {
                if (i == 0 && this.headDeteched)
                {
                    branchNames.Add(Constants.DetachedHeadBranchName);
                }
                else
                {
                    branchNames.Add(this.branches[i].FriendlyName);
                }
            }

            return branchNames.ToArray();
        }

        public bool SelectBranch(int branchIndex)
        {
            if (branchIndex < 0 || branchIndex >= this.branches.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(branchIndex));
            }

            if (branchIndex == this.currentBranchIndex) return false;

            this.currentBranchIndex = branchIndex;
            this.cachedCommits = null;
            return true;
        }

        public void ShowDiffWithHead(Commit commit)
        {
            ShowDiff(commit, vsHead: true);
        }

        public void ShowDiff(Commit commit, string fileRelPath = null, bool forEntireCommit = false, bool vsHead = false)
        {
            string root = Path.Combine(Path.GetTempPath(), "gitw", Path.GetRandomFileName().Substring(0, 4));
            string proot = Path.Combine(root, "a");
            string croot = Path.Combine(root, "b");

            if (Directory.Exists(root))
            {
                Directory.Delete(root, true);
            }
            Directory.CreateDirectory(proot);
            Directory.CreateDirectory(croot);

            if (fileRelPath == null && !forEntireCommit)
            {
                fileRelPath = this.targetPath;
            }

            var patch = GetCommitPatch(commit, vsHead);
            if (string.IsNullOrEmpty(fileRelPath) || fileRelPath.EndsWith("\\", StringComparison.Ordinal))
            {
                foreach (var pe in patch)
                {
                    this.repo.CopyBlobToPath(pe.Oid, Path.Combine(croot, pe.Path));
                    this.repo.CopyBlobToPath(pe.OldOid, Path.Combine(proot, pe.Path));
                }
            }
            else
            {
                // patch[fileRelPath] only works for new path, so loop over all
                // patch entries and compare old path as well.
                foreach (var pe in patch)
                {
                    if (fileRelPath == pe.Path || fileRelPath == pe.OldPath)
                    {
                        this.repo.CopyBlobToPath(pe.Oid, Path.Combine(croot, pe.Path));
                        this.repo.CopyBlobToPath(pe.OldOid, Path.Combine(proot, pe.Path));
                        break;
                    }
                }
            }

            StartDiffCmd(proot, croot, root);
        }

        public Patch GetCommitPatch(Commit commit, bool vsHead = false)
        {
            var parent = vsHead ? commit : commit.Parents.FirstOrDefault();
            commit = vsHead ? this.repo.Head.Tip : commit;
            var patch = this.repo.Diff.Compare<Patch>(parent?.Tree, commit.Tree);
            return patch;
        }

        private static bool IsEqualTreeEntry(TreeEntry left, TreeEntry right)
        {
            return (left == null && right == null) ||
                   (left != null && right != null && left.Target.Sha == right.Target.Sha);
        }

        private void ParseDiffTool()
        {
            string difftool = repo.Config.Get<string>("diff.guitool")?.Value ??
                              repo.Config.Get<string>("diff.tool")?.Value;
            if (difftool != null)
            {
                this.diffCmd = repo.Config.Get<string>($"difftool.{difftool}.cmd")?.Value;

                // Git has a list of supported diff tools with known command line, defined in the
                // mergetools folder under the git source root. For example, winmerge is
                // invoked like: "$merge_tool_path" -u -e "$LOCAL" "$REMOTE"
                // For simplicity we just use the most common command line for all such tools,
                // i.e. "$merge_tool_path" "$LOCAL" "$REMOTE"
                if (this.diffCmd == null)
                {
                    string path = repo.Config.Get<string>($"difftool.{difftool}.path")?.Value;
                    if (path != null)
                    {
                        this.diffCmd = string.Format($"\"{path}\" \"$LOCAL\" \"$REMOTE\"");
                    }
                }

                if (this.diffCmd != null)
                {
                    string cmd = this.diffCmd.Trim();
                    if (cmd.StartsWith("\"", StringComparison.Ordinal))
                    {
                        int nextQuoteIndex = cmd.IndexOf('"', 1);
                        if (nextQuoteIndex >= 0)
                        {
                            this.diffExe = cmd.Substring(0, nextQuoteIndex + 1);
                        }
                    }
                    else
                    {
                        int index = cmd.IndexOf(' ', 0);
                        if (index < 0) index = cmd.Length;
                        this.diffExe = cmd.Substring(0, index);
                    }

                    if (this.diffExe != null)
                    {
                        this.diffArguments = cmd.Substring(this.diffExe.Length);
                    }
                }
            }
        }

        private IEnumerable<Commit> FetchCommits(int count)
        {
            if (this.repo == null) return new List<Commit>();

            if (this.targetPath.Length == 0)
            {
                return this.branches[this.currentBranchIndex].Commits.Where(c => CommitHasOneParent(c)).Take(count);
            }
            else
            {
                string relPath = this.targetPath;
                var segments = relPath.Split(PathSeparators, StringSplitOptions.RemoveEmptyEntries);
                // Without the filter libgit2sharp/FileHistory will throw KeyNotFoundException
                // on certain history e.g. containing merges.
                var filter = new CommitFilter()
                {
                    SortBy = CommitSortStrategies.Topological,
                    //FirstParentOnly = true,
                };

                return this.branches[this.currentBranchIndex].Commits.Where(c => CommitHasOneParent(c) && CommitTouchedPath(c, segments)).Take(count);
            }
        }

        private bool CommitHasOneParent(Commit commit)
        {
            return !commit.Parents.Skip(1).Any();
        }

        private bool CommitTouchedPath(Commit commit, string[] segments)
        {
            var te = GetTreeEntryForPathSegments(commit, segments);
            var pte = GetTreeEntryForPathSegments(commit.Parents?.FirstOrDefault(), segments);

            return !IsEqualTreeEntry(te, pte);
        }

        private TreeEntry GetTreeEntryForPathSegments(Commit commit, string[] segments)
        {
            var tree = commit?.Tree;
            TreeEntry te = null;

#if USE_COMMIT_MAP
            if (this.map.TryGetValue(commit, out te))
            {
                return te;
            }
#endif

            foreach (var segment in segments)
            {
                if (tree == null) return null;

                te = tree[segment];
                if (te == null) return null;

                tree = te.TargetType == TreeEntryTargetType.Tree ? (Tree)te.Target : null;
            }

#if USE_COMMIT_MAP
            this.map.Add(commit, te);
#endif

            return te;
        }

        private void StartDiffCmd(string left, string right, string root)
        {
            if (this.diffExe == null) return;

            var p = new Process();
            p.StartInfo.FileName = this.diffExe;
            p.StartInfo.Arguments = this.diffArguments?.Replace("$LOCAL", left).Replace("$REMOTE", right);
            p.EnableRaisingEvents = true;
            p.Exited += new EventHandler(
                (sender, e) =>
                {
                    p.Dispose();
                    if (root != null && Directory.Exists(root))
                    {
                        Directory.Delete(root, true);
                    }
                });
            p.Start();
        }
    }
}
