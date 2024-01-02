using Comet.Launcher.States;
using Comet.Network.Packets.Updater;

namespace Comet.Launcher.Packets
{
    internal sealed class MsgUpdCheckHash : MsgUpdCheckHash<Server>
    {
        /// <inheritdoc />
        public override async Task ProcessAsync(Server client)
        {
        }
    }
}