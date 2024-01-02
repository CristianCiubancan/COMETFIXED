using System.Threading.Tasks;
using Comet.Account.States;
using Comet.Network.Packets.Internal;

namespace Comet.Account.Packets
{
    public sealed class MsgAccServerAction : MsgAccServerAction<GameServer>
    {
        public override Task ProcessAsync(GameServer client)
        {
            return Task.CompletedTask;
        }
    }
}