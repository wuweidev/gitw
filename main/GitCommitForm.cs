using System.Windows.Forms;
using LibGit2Sharp;

namespace gitw
{
    public partial class GitCommitForm : Form
    {
        private GitCommitListView listView;

        public GitCommitForm(GitLog gitLog, Commit commit)
        {
            InitializeComponent();

            var commitOwner = new GitCommitListViewOwner(gitLog, commit);
            var commitListView = new GitCommitListView(commitOwner);

            SuspendLayout();

            this.Text += $" {commit.Sha}";
            this.Width = 1200;
            this.Height = 600;

            this.listView = commitListView;
            this.listView.Dock = DockStyle.Fill;

            this.Controls.Add(this.listView);

            ResumeLayout(false);
            PerformLayout();
        }
    }
}
