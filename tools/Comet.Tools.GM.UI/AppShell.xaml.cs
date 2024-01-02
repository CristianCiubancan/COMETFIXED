namespace Comet.Tools.GM.UI
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            BindingContext = new ShellViewModel();
        }
    }
}