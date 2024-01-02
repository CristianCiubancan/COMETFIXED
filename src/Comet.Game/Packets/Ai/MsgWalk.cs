using System.Threading.Tasks;
using Comet.Game.Internal.AI;
using Comet.Game.States;
using Comet.Game.World.Managers;
using Comet.Network.Packets;
using Comet.Network.Packets.Game;

namespace Comet.Game.Packets.Ai
{
    public sealed class MsgWalk : MsgWalk<AiClient>
    {
        /// <summary>
        ///     Process can be invoked by a packet after decode has been called to structure
        ///     packet fields and properties. For the server implementations, this is called
        ///     in the packet handler after the message has been dequeued from the server's
        ///     <see cref="PacketProcessor{TClient}" />.
        /// </summary>
        /// <param name="client">Client requesting packet processing</param>
        public override async Task ProcessAsync(AiClient client)
        {
            Role target = RoleManager.GetRole(Identity);
            if (target == null)
                return;

            await target.ProcessOnMoveAsync();
            await target.MoveTowardAsync(Direction, Mode);
            await target.BroadcastRoomMsgAsync(this, false);
        }
    }
}