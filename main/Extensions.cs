using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using LibGit2Sharp;

namespace gitw
{
    public static class Extensions
    {
        #region Windows forms extensions

        public static bool ClipboardCopyItem(this ListView lv)
        {
            if (lv.SelectedIndices.Count == 0) return false;

            var item = lv.Items[lv.SelectedIndices[0]];
            string.Join("\t", item.SubItems.Cast<ListViewItem.ListViewSubItem>().Select(si => si.Text).ToArray())
                  .CopyToClipboard();
            return true;
        }

        public static void CopyToClipboard(this string text)
        {
            for (int i = 0; i < 10; ++i)
            {
                try
                {
                    Clipboard.SetText(text);
                    break;
                }
                catch (ExternalException)
                {
                }
            }
        }

        public static void SetCueText(this TextBox textBox, string text)
        {
            Win32Native.SendMessage(textBox.Handle, Win32Native.EM_SETCUEBANNER, 1, text);
        }

        public static int GetHeaderHeight(this ListView lv)
        {
            var rc = new Win32Native.RECT();
            var hwnd = Win32Native.SendMessage(lv.Handle, Win32Native.LVM_GETHEADER, 0, 0);
            if (hwnd != null)
            {
                Win32Native.GetWindowRect(hwnd, out rc);
            }
            return rc.Bottom - rc.Top;
        }

        public static void SetExplorerTheme(this Control control)
        {
            Win32Native.SetWindowTheme(control.Handle, "Explorer", null);
        }

        #endregion

        #region Git2sharp extensions

        public static void CopyBlobToPath(this Repository repo, ObjectId oid, string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            if (oid != null)
            {
                var blob = repo.Lookup<Blob>(oid);
                if (blob != null)
                {
                    using (var fs = File.OpenWrite(path))
                    using (var bs = blob.GetContentStream())
                    {
                        bs.CopyTo(fs);
                        return;
                    }
                }
            }

            File.WriteAllText(path, string.Empty);
        }

        public static string[] ToListViewStrings(this Commit commit)
        {
            return new string[]
            {
                commit.Sha,
                commit.Author.When.ToLocalTimeString(),
                commit.Author.ToString(),
                commit.MessageShort,
            };
        }

        public static ListViewItem ToListViewItem(this Commit commit, string[] subItems)
        {
            return new ListViewItem(subItems)
            {
                Tag = commit,
            };
        }

        public static ListViewItem ToListViewItem(this Commit commit)
        {
            return commit.ToListViewItem(commit.ToListViewStrings());
        }

        public static void FilterToListViewItems(this IEnumerable<Commit> commits, string text, Signature author, IList<ListViewItem> items)
        {
            if (items == null) return;

            foreach (var c in commits)
            {
                var subItems = c.ToListViewStrings();

                if ((string.IsNullOrEmpty(text) || subItems.Any(s => s.ContainsIgnoreCase(text))) &&
                    (author == null || c.Author.Email == author.Email))
                {
                    items.Add(c.ToListViewItem(subItems));
                }
            }

        }

        #endregion

        #region General extensions

        public static void AddToList<T>(this IEnumerable<T> items, IList<T> list, CancellationToken token)
        {
            foreach (var item in items)
            {
                if (token.IsCancellationRequested) return;
                list.Add(item);
            }
        }

        public static string ToLocalTimeString(this DateTimeOffset time)
        {
            return time.ToLocalTime().ToString("G");
        }

        public static bool ContainsIgnoreCase(this string s, string value)
        {
            return s.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        #endregion
    }
}
