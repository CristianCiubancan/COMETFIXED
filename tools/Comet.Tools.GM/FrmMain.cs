using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Comet.Tools.GM.Threading;

namespace Comet.Tools.GM
{
    public partial class FrmMain : Form
    {
        public static FrmMain Instance { get; private set; }

        private readonly UsualTaskThread mMainThread;

        public FrmMain()
        {
            Instance = this;

            InitializeComponent();

            mMainThread = new UsualTaskThread(OnDisconnectAsync);
            _ = mMainThread.StartAsync();
        }
        

        #region Login Screen

        public async Task DisplayLoginScreenAsync()
        {
            if (HasLoginDialog())
            {
                FrmLogin login = GetLoginDialog();
                login.Show(this);
                return;
            }

            FrmLogin frm = new FrmLogin();
            frm.MdiParent = this;
            frm.Show();
        }

        public void CloseAllWork()
        {
            foreach (Form ctrl in MdiChildren)
            {
                ctrl.Close();
            }
        }

        public async Task OpenDialogAsync(Form form, bool isLoginRequired = true)
        {
            if (isLoginRequired)
            {
                await DisplayLoginScreenAsync();
                return;
            }

            if (form == null)
                return;

            if (HasDialog(form))
            {
                return;
            }
            
            form.MdiParent = this;
            form.Show();
        }

        public bool HasLoginDialog()
        {
            return MdiChildren.Any(x => x.GetType() == typeof(FrmLogin));
        }

        public FrmLogin GetLoginDialog()
        {
            return MdiChildren.FirstOrDefault(x => x.GetType() == typeof(FrmLogin)) as FrmLogin;
        }

        public bool HasDialog(Form form)
        {
            return MdiChildren.Any(x => x.GetType() == form.GetType());
        }

        public Form GetDialog(Type formType)
        {
            return MdiChildren.FirstOrDefault(x => x.GetType() == formType);
        }

        #endregion

        #region Socket

        private Task OnDisconnectAsync()
        {
            return Task.CompletedTask;
        }

        #endregion

        #region Form Close

        private async void FrmMain_FormClosed(object sender, FormClosedEventArgs e)
        {
            await mMainThread.CloseAsync();
        }

        private void FrmMain_ResizeEnd(object sender, EventArgs e)
        {
            if (HasLoginDialog())
            {
                FrmLogin login = GetLoginDialog();
                login.Left = (ClientRectangle.Width - login.Width) / 2;
                login.Top = (ClientRectangle.Height - login.Height) / 2;
            }
        }

        #endregion
    }
}