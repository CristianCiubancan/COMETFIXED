using Comet.Tools.GM.UI.Models;
using Comet.Tools.GM.UI.Resources.Strings;

namespace Comet.Tools.GM.UI.ViewModels
{
    public class ShellViewModel
    {
        public AppSection Login { get; set; }
        public AppSection Dashboard { get; set; }
        public AppSection About { get; set; }

        public ShellViewModel()
        {
            Login = new AppSection() { Title = AppResource.SignIn, Icon = "discover.png", IconDark = "discover_dark.png", TargetType = typeof(LoginPage), MustBeSigned = false };
            Dashboard = new AppSection() { Title = AppResource.Dashboard, Icon = "discover.png", IconDark = "discover_dark.png", TargetType = typeof(DashboardPage) };
            About = new AppSection() { Title = AppResource.About, Icon = "discover.png", IconDark = "discover_dark.png", TargetType = typeof(AboutPage) };
        }
    }
}
