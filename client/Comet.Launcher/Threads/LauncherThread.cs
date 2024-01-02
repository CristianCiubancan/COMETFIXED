using Comet.Launcher.Configuration;
using Comet.Launcher.Helpers;
using Comet.Launcher.Managers;
using Comet.Launcher.Packets;
using Comet.Launcher.States;
using Comet.Shared;

namespace Comet.Launcher.Threads
{
    public sealed class LauncherThread : TimerBase
    {
        private const int PingTimeoutSecs = 10;

        private readonly FrmMain mForm;
        private readonly TimeOut mPingTimeOut;

        private Client mClient;
        private Server mServer;

        /// <inheritdoc />
        public LauncherThread(FrmMain form)
            : base(1000, "LauncherThread")
        {
            mForm = form;

            mPingTimeOut = new TimeOut(PingTimeoutSecs);
        }

        /// <inheritdoc />
        protected override Task OnStartAsync()
        {
            if (OperatingSystemHelper.IsWindows7())
            {
                MessageBox.Show(mForm, LocaleManager.GetString("StrWindows7Alert"),
                                LocaleManager.GetString("StrWindows7AlertTitle"), MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);
            }
            else if (OperatingSystemHelper.IsWindows7OrHigher() && Kernel.UserConfiguration.WindowsDefendAlert == null)
            {
                DialogResult result = MessageBox.Show(mForm, LocaleManager.GetString("StrWindows10Alert"),
                                                      LocaleManager.GetString("StrWindows10AlertTitle"),
                                                      MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (result == DialogResult.Yes)
                {
                    Kernel.OpenUrl("https://worldconqueronline.tawk.help/article/resolvendo-problemas-de-instalação");
                }

                Kernel.UserConfiguration.WindowsDefendAlert = DateTime.Now;
                Kernel.UserConfiguration.Save();
            }

            return base.OnStartAsync();
        }

        /// <inheritdoc />
        protected override async Task<bool> OnElapseAsync()
        {
            if (mClient == null)
            {
                mForm.SetProgressLabel(LocaleManager.GetString("StrAttemptingConnection"));
                foreach (string fullAddress in Kernel.ClientConfiguration.Addresses)
                {
                    string[] addresses = fullAddress.Split(':');
                    if (addresses.Length != 2)
                        continue;

                    string address = addresses[0];
                    if (!int.TryParse(addresses[1], out int port))
                        continue;

                    Client client = new(this);
                    if (!await client.ConnectToAsync(address, port))
                        continue;

                    mForm.SetProgressLabel(LocaleManager.GetString("StrConnectedToServer"));
                    mClient = client;

                    mPingTimeOut.Startup(PingTimeoutSecs);
                    break;
                }
            }
            else if (mServer != null && !mServer.Socket.Connected)
            {
                mServer.Disconnect();
                mPingTimeOut.Clear();
            }

            if (mPingTimeOut.IsActive() && mPingTimeOut.ToNextTime() && mServer?.Socket.Connected == true)
            {
                await mServer.SendAsync(new MsgUpdPing());
            }

            return true;
        }

        /// <inheritdoc />
        protected override Task OnCloseAsync()
        {
            return base.OnCloseAsync();
        }

        public async Task OnConnectAsync(Server server)
        {
            mForm.SetProgressLabel("StrHandsakeUpdater");
            mServer = server;
            var msg = new MsgUpdHandshake(server.DiffieHellman.PublicKey, server.DiffieHellman.Modulus, null, null);
            await server.SendAsync(msg);
        }

        public async Task OnDisconnectAsync()
        {
            mForm.SetProgressLabel(LocaleManager.GetString("StrDisconnectedFromServer"));
            mForm.UpdatePingMsg(0, -1);
            mPingTimeOut.Clear();

            mClient = null;
            mServer = null;
        }

        #region UI

        public async Task RequestUpdatesAsync()
        {
            await mServer.SendAsync(new MsgUpdQueryVersion
            {
                ClientVersion = FrmMain.CURRENT_VERSION,
                GameVersion = FrmMain.CurrentGameVersion
            });
        }

        #endregion
    }
}