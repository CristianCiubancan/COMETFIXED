using System.Threading.Tasks;
using Comet.Game.Internal.AI;
using Comet.Game.States;
using Comet.Game.World.Managers;
using Comet.Network.Packets;
using Comet.Shared;

namespace Comet.Game.Packets.Ai
{
    /// <remarks>Packet Type 1010</remarks>
    /// <summary>
    ///     Message containing a general action being performed by the client. Commonly used
    ///     as a request-response protocol for question and answer like exchanges. For example,
    ///     walk requests are responded to with an answer as to if the step is legal or not.
    /// </summary>
    public sealed class MsgAction : MsgAction<AiClient>
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
            Role role = RoleManager.GetRole(Identity);
            if (role == null) return;

            switch (Action)
            {
                case ActionType.MapJump: // 133
                {
                    var newX = (ushort) Command;
                    var newY = (ushort) (Command >> 16);

                    await role.ProcessOnMoveAsync();
                    await role.JumpPosAsync(newX, newY);
                    await role.BroadcastRoomMsgAsync(this, true);
                    await role.ProcessAfterMoveAsync();
                    break;
                }


                default:
                {
                    await Log.WriteLogAsync(LogLevel.Warning,
                                            "Missing packet {0}, Action {1}, Length {2}\n{3}",
                                            Type, Action, Length, PacketDump.Hex(Encode()));
                    break;
                }
            }
        }
    }
}