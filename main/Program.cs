using System;
using System.IO;
using System.Windows.Forms;
using LibGit2Sharp;

namespace gitw
{
    public static class Program
    {
        public static GitApplicationContext AppContext;
        public static WindowsFormsSynchronizationContext SyncContext;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            AppContext = new GitApplicationContext();
            SyncContext = new WindowsFormsSynchronizationContext();

            string pathOrCommitish = args.Length > 0 ? args[0] : null;
            string fullPath = pathOrCommitish == null ? Environment.CurrentDirectory : Path.GetFullPath(pathOrCommitish);

            if (Directory.Exists(fullPath) && !fullPath.EndsWith("\\"))
            {
                fullPath += "\\";
            }

            // Repository.Discover doesn't work with paths that don't exist.
            // Traverse up the tree to find an existing directory.
            string discoverPath = fullPath;
            while (!Directory.Exists(discoverPath))
            {
                discoverPath = Directory.GetParent(discoverPath).FullName;
            }

            string repoRoot = Repository.Discover(discoverPath);
            if (repoRoot == null) return;

            using (var repo = new Repository(repoRoot))
            {
                Form form;
                var commit = TryLookupCommit(repo, fullPath, pathOrCommitish);
                if (commit != null)
                {
                    fullPath = Environment.CurrentDirectory;
                    if (!fullPath.EndsWith("\\"))
                    {
                        fullPath += "\\";
                    }

                    var gitLog = new GitLog(repo, fullPath, Constants.MaxCommits);
                    form = new GitCommitForm(gitLog, commit);
                }
                else
                {
                    form = new GitLogForm(repo, fullPath);
                }

                AppContext.NewForm(form);

                Application.Run(AppContext);
            }
        }

        static Commit TryLookupCommit(Repository repo, string fullPath, string commitish)
        {
            return commitish == null || File.Exists(fullPath) || Directory.Exists(fullPath) ?
                null : repo.Lookup<Commit>(commitish);
        }
    }
}
