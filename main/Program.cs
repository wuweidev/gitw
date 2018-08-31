﻿using System;
using System.IO;
using System.Windows.Forms;
using LibGit2Sharp;

namespace gitw
{
    public static class Program
    {
        public static GitApplicationContext AppContext;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            AppContext = new GitApplicationContext();

            string path = args.Length > 0 ? args[0] : null;
            string fullPath = path == null ? Environment.CurrentDirectory : Path.GetFullPath(path);

            if (Directory.Exists(fullPath))
            {
                fullPath += "\\";
            }

            string repoRoot = Repository.Discover(fullPath);
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
