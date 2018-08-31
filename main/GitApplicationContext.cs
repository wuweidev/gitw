using System.Windows.Forms;

namespace gitw
{
    public class GitApplicationContext : ApplicationContext
    {
        private int formCount;

        public GitApplicationContext()
        {
        }

        public void NewForm(Form form)
        {
            this.formCount++;

            form.FormClosed += new FormClosedEventHandler(
                (sender, e) =>
                {
                    this.formCount--;
                    if (this.formCount == 0)
                    {
                        ExitThread();
                    }
                });

            form.Show();
        }
    }
}
