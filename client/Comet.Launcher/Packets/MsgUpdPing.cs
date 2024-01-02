using Comet.Launcher.States;
using Comet.Network.Packets.Updater;

namespace Comet.Launcher.Packets
{
    internal sealed class MsgUpdPing : MsgUpdPing<Server>
    {
        /// <inheritdoc />
        public override Task ProcessAsync(Server client)
        {
            FrmMain.Instance.UpdatePingMsg(Timestamp, Environment.TickCount);
            return base.ProcessAsync(client);
        }
    }
}