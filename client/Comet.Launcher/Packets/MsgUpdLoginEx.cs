using Comet.Launcher.Configuration;
using Comet.Launcher.Managers;
using Comet.Launcher.States;
using Comet.Network.Packets.Updater;

namespace Comet.Launcher.Packets
{
    internal sealed class MsgUpdLoginEx : MsgUpdLoginEx<Server>
    {
        /// <inheritdoc />
        public override async Task ProcessAsync(Server client)
        {

            switch (Response)
            {
                case UpdLoginEx.IncompatibleWindowsVersion:
                case UpdLoginEx.Success:
                {
                    FrmMain.Instance.SetProgressLabel(LocaleManager.GetString("StrLoginSuccessfull"));

                    if (Response == UpdLoginEx.IncompatibleWindowsVersion && !Kernel.UserConfiguration.SuppressIncorrectWindowsVersion)
                    {
                        MessageBox.Show(LocaleManager.GetString("StrIncompatibleWindowsVersion"), string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        Kernel.UserConfiguration.SuppressIncorrectWindowsVersion = true;
                    }

                    await Task.Delay(1500);
                    FrmMain.Instance.SetProgressLabel(LocaleManager.GetString("StrFetchingUpdates"));

                    await client.SendAsync(new MsgUpdQueryVersion
                    {
                        ClientVersion = FrmMain.CURRENT_VERSION,
                        GameVersion = FrmMain.CurrentGameVersion
                    });
                    break;
                }
                default:
                {
                    MessageBox.Show(LocaleManager.GetString($"StrError{Response}"));
                    client.Disconnect();
                    break;
                }
            }

        }
    }
}