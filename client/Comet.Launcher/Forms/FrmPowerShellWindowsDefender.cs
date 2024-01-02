using Comet.Launcher.Managers;

namespace Comet.Launcher.Forms
{
    public partial class FrmPowerShellWindowsDefender : Form
    {
        public FrmPowerShellWindowsDefender()
        {
            InitializeComponent();
        }

        private void FrmPowerShellWindowsDefender_Load(object sender, EventArgs e)
        {
            foreach (Control ctrl in Controls)
            {
                ctrl.Text = LocaleManager.GetString(ctrl.Text);
            }
        }
    }
}
