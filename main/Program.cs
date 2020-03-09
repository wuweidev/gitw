using System;
using System.IO;
using System.Windows.Forms;
using LibGit2Sharp;

namespace gitw
{
    public static class Program
    {
        // TODO: show hyperlink in commit view
        // TODO: command line to show commit view directly
        // TODO: shift tab not working properly

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

            string path = args.Length > 0 ? args[0] : null;
            string fullPath = path == null ? Environment.CurrentDirectory : Path.GetFullPath(path);

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
                var form = new GitLogForm(repo, fullPath);

                AppContext.NewForm(form);

                Application.Run(AppContext);
            }
        }
    }
}
