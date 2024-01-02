using System.Threading.Tasks;
using Comet.Game.States;
using Comet.Game.World.Managers;
using Comet.Network.Packets;
using Comet.Network.Packets.Game;
using Comet.Shared;

namespace Comet.Game.Packets
{
    public sealed class MsgMentorPlayer : MsgMentorPlayer<Client>
    {
        public override async Task ProcessAsync(Client client)
        {
            Character sender = RoleManager.GetUser(SenderId);
            Character target = RoleManager.GetUser(TargetId);

            if (sender == null || client.Character.Identity != sender.Identity || target == null)
                return;

            if (Action == 0) // Action 0 Enlight
            {
                await sender.EnlightenPlayerAsync(target);

                await target.BroadcastRoomMsgAsync(new MsgMentorPlayer
                {
                    SenderId = sender.Identity,
                    TargetId = target.Identity
                }, true);
            }
            else if (Action == 1) // Worship
            {
                // do nothing?
            }
            else
            {
                await Log.WriteLogAsync($"Unhandled Action {Action} for MsgMentorPlayer");
                await Log.WriteLogAsync(LogLevel.Socket, PacketDump.Hex(Encode()));
            }
        }
    }
}