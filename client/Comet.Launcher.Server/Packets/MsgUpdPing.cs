using System.Threading.Tasks;
using Comet.Launcher.Server.States;
using Comet.Network.Packets.Updater;

namespace Comet.Launcher.Server.Packets
{
    internal sealed class MsgUpdPing : MsgUpdPing<Client>
    {
        /// <inheritdoc />
        public override Task ProcessAsync(Client client)
        {
            return client.SendAsync(this);
        }
    }
}