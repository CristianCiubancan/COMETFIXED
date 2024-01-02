using Comet.Launcher.Managers;
using Comet.Launcher.States;
using Comet.Network.Packets.Updater;

namespace Comet.Launcher.Packets
{
    internal sealed class MsgUpdConfirmHash : MsgUpdConfirmHash<Server>
    {
        /// <inheritdoc />
        public override async Task ProcessAsync(Server client)
        {
            if (Result != SUCCESS)
            {
                FrmMain.Instance.FinishProcess(false);
                FrmMain.Instance.SetProgressLabel(LocaleManager.GetString("StrHashConfirmationFailed"));
                client.Disconnect();
                FrmMain.Instance.ForceClose();
                return;
            }

            FrmMain.Instance.SetProgressLabel(LocaleManager.GetString("StrUpdateComplete"));
            FrmMain.Instance.FinishProcess(true);
        }
    }
}
