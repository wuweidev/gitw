using System;
using System.Drawing;
using System.Windows.Forms;
using LibGit2Sharp;

namespace gitw
{
    public partial class GitLogForm : Form
    {
        private GitLogListView listView;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel commitCountLabel;
        private ToolStripTextBox searchBox;
        private ToolStripStatusLabel authorLabel;
        private ToolStripStatusLabel fromDateLabel;
        private ToolStripStatusLabel toDateLabel;
        private ToolStripStatusLabel fillerLabel;
        private ToolStripComboBox branchComboBox;
        private Timer searchBoxTimer;

        private int selectedBranchIndex;

        public GitLogForm(Repository repo, string path)
        {
            InitializeComponent();

            var gitLog = new GitLog(repo, path, Constants.MaxCommits);
            var logOwner = new GitLogListViewOwner(gitLog);
            var logListView = new GitLogListView(logOwner);

            this.listView = logListView;
            this.statusStrip = new StatusStrip();
            this.commitCountLabel = new ToolStripStatusLabel();
            this.searchBox = new ToolStripTextBox();
            this.authorLabel = new ToolStripStatusLabel();
            this.fromDateLabel = new ToolStripStatusLabel();
            this.toDateLabel = new ToolStripStatusLabel();
            this.fillerLabel = new ToolStripStatusLabel();
            this.branchComboBox = new ToolStripComboBox();

            this.statusStrip.SuspendLayout();
            SuspendLayout();

            this.Text += $" {path}";
            this.Width = 1200;
            this.Height = 600;
            this.KeyPreview = true;
            this.KeyDown += GitLogForm_KeyDown;

            this.listView.Dock = DockStyle.Fill;
            this.listView.ListSizeChanged += ListView_ListSizeChanged;
            this.listView.FilteringByAuthor += ListView_FilteringByAuthor;
            this.listView.FilteringFromDate += ListView_FilteringFromDate;
            this.listView.FilteringToDate += ListView_FilteringToDate;

            this.statusStrip.Items.Add(this.commitCountLabel);
            this.statusStrip.Items.Add(this.searchBox);
            this.statusStrip.Items.Add(this.authorLabel);
            this.statusStrip.Items.Add(this.fromDateLabel);
            this.statusStrip.Items.Add(this.toDateLabel);
            this.statusStrip.Items.Add(this.fillerLabel);
            this.statusStrip.Items.Add(this.branchComboBox);
            this.statusStrip.TabStop = true;
            this.statusStrip.ShowItemToolTips = true;

            this.commitCountLabel.AutoSize = false;
            this.commitCountLabel.Size = new Size(150, 20);
            this.commitCountLabel.TextAlign = ContentAlignment.MiddleLeft;
            this.commitCountLabel.BorderSides = ToolStripStatusLabelBorderSides.Right;
            this.commitCountLabel.BorderStyle = Border3DStyle.Etched;

            this.authorLabel.Size = new Size(180, 16);
            this.authorLabel.ToolTipText = Constants.AuthorLabelToolTipText;
            this.authorLabel.AutoToolTip = true;
            this.authorLabel.Click += AuthorLabel_Click;

            this.fromDateLabel.Size = new Size(150, 16);
            this.fromDateLabel.ToolTipText = Constants.FromDateLabelToolTipText;
            this.fromDateLabel.AutoToolTip = true;
            this.fromDateLabel.Click += FromDateLabel_Click;

            this.toDateLabel.Size = new Size(150, 16);
            this.toDateLabel.ToolTipText = Constants.ToDateLabelToolTipText;
            this.toDateLabel.AutoToolTip = true;
            this.toDateLabel.Click += ToDateLabel_Click;

            this.fillerLabel.Spring = true;

            this.branchComboBox.Size = new Size(180, 16);
            this.branchComboBox.AutoSize = true;
            this.branchComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            this.branchComboBox.FlatStyle = FlatStyle.Standard;
            this.branchComboBox.Padding = new Padding(0, 0, 20, 0);
            this.branchComboBox.Items.AddRange(gitLog.GetLocalBranchNames(out this.selectedBranchIndex));
            this.branchComboBox.SelectedIndex = this.selectedBranchIndex;
            this.branchComboBox.SelectedIndexChanged += BranchComboBox_SelectedIndexChanged;

            this.searchBox.AutoSize = false;
            this.searchBox.Font = new Font(Constants.ListViewFontName, 8);
            this.searchBox.Size = new Size(180, 16);
            this.searchBox.Padding = new Padding(10, 0, 0, 0);
            this.searchBox.AutoCompleteMode = AutoCompleteMode.Append;
            this.searchBox.AutoCompleteSource = AutoCompleteSource.CustomSource;
            this.searchBox.MaxLength = Constants.SearchBoxMaxLength;
            this.searchBox.TextBox.SetCueText(Constants.SearchBoxCueText);
            this.searchBox.TextChanged += SearchBox_TextChanged;
            this.searchBox.KeyDown += SearchBox_KeyDown;
            this.searchBox.TextBox.PreviewKeyDown += TextBox_PreviewKeyDown;

            this.searchBoxTimer = new Timer();
            this.searchBoxTimer.Tick += SearchBoxTimer_Tick;
            this.searchBoxTimer.Interval = Constants.SearchBoxTimerInterval;

            this.Controls.Add(this.listView);
            this.Controls.Add(this.statusStrip);

            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        protected override bool ProcessTabKey(bool forward)
        {
            // Shift tab from search box goes to branch combo box if we don't override this.
            if (this.ActiveControl == this.searchBox.Control && !forward)
            {
                this.listView.Focus();
                return true;
            }
            return base.ProcessTabKey(forward);
        }

        private void BranchComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.selectedBranchIndex == this.branchComboBox.SelectedIndex) return;

            this.selectedBranchIndex = this.branchComboBox.SelectedIndex;
            this.listView.SelectBranch(this.branchComboBox.SelectedItem as string);
        }

        private void GitLogForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Handled) return;

            if (e.KeyData == (Keys.Control | Keys.I))
            {
                e.Handled = true;
                if (!this.searchBox.Focused)
                {
                    this.searchBox.Focus();
                }
            }
            else if (e.KeyData == Keys.Escape)
            {
                e.Handled = true;
                ClearFiltering();
            }
        }

        private void ListView_ListSizeChanged(object sender, EventArgs e)
        {
            this.commitCountLabel.Text =
                this.listView.VirtualListSize == 0 ?  string.Empty :
                this.listView.VirtualListSize == 1 ?  "1 commit" : $"{this.listView.VirtualListSize} commits";
        }

        private void ListView_FilteringByAuthor(object sender, FilteringByAuthorEventArgs e)
        {
            this.authorLabel.Text = string.IsNullOrEmpty(e.AuthorName) ? string.Empty : $"Author: {e.AuthorName}";
        }

        private void ListView_FilteringFromDate(object sender, FilteringFromDateEventArgs e)
        {
            this.fromDateLabel.Text = e.FromDate.HasValue ? $"From: {e.FromDate.Value.ToLocalTimeString()}" : string.Empty;
        }

        private void ListView_FilteringToDate(object sender, FilteringToDateEventArgs e)
        {
            this.toDateLabel.Text = e.ToDate.HasValue ? $"To: {e.ToDate.Value.ToLocalTimeString()}" : string.Empty;
        }

        private void AuthorLabel_Click(object sender, EventArgs e)
        {
            this.listView.CancelFilterByAuthor();
        }

        private void FromDateLabel_Click(object sender, EventArgs e)
        {
            this.listView.CancelFilterFromDate();
        }

        private void ToDateLabel_Click(object sender, EventArgs e)
        {
            this.listView.CancelFilterToDate();
        }

        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Handled) return;

            if (e.KeyData == Keys.Enter)
            {
                e.Handled = true;
                FilterBySearchText();
            }
        }

        private void TextBox_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (e.KeyData == Keys.Escape)
            {
                ClearFiltering();
            }
        }

        private void SearchBox_TextChanged(object sender, EventArgs e)
        {
            this.searchBoxTimer.Stop();
            this.searchBoxTimer.Start();
        }

        private void SearchBoxTimer_Tick(object sender, EventArgs e)
        {
            FilterBySearchText();
        }

        private void FilterBySearchText()
        {
            this.searchBoxTimer.Stop();
            this.listView.FilterBySearchText(this.searchBox.Text);
        }

        private void ClearFiltering()
        {
            this.searchBox.Clear();
            this.searchBoxTimer.Stop();
            this.listView.ClearFiltering();

            if (this.searchBox.Focused)
            {
                this.listView.Focus();
            }
        }
    }
}
