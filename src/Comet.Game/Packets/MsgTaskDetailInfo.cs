using System.Threading.Tasks;
using Comet.Game.States;
using Comet.Network.Packets.Game;

namespace Comet.Game.Packets
{
    public sealed class MsgTaskDetailInfo : MsgTaskDetailInfo<Client>
    {
        public override async Task ProcessAsync(Client client)
        {
        }
    }
}