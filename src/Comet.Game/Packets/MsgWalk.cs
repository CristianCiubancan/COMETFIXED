using System.Threading.Tasks;
using Comet.Game.States;
using Comet.Game.World.Managers;
using Comet.Network.Packets;
using Comet.Network.Packets.Game;

namespace Comet.Game.Packets
{
    public sealed class MsgWalk : MsgWalk<Client>
    {
        /// <summary>
        ///     Process can be invoked by a packet after decode has been called to structure
        ///     packet fields and properties. For the server implementations, this is called
        ///     in the packet handler after the message has been dequeued from the server's
        ///     <see cref="PacketProcessor{TClient}" />.
        /// </summary>
        /// <param name="client">Client requesting packet processing</param>
        public override async Task ProcessAsync(Client client)
        {
            if (client != null && Identity == client.Character.Identity)
            {
                Character user = client.Character;
                await user.ProcessOnMoveAsync();

                bool moved = await user.MoveTowardAsync(Direction, Mode);
                Character couple;
                if (moved 
                    && user.HasCoupleInteraction() 
                    && user.HasCoupleInteractionStarted()
                    && (couple = user.GetCoupleInteractionTarget()) != null)
                {
                    await couple.ProcessOnMoveAsync();

                    couple.MapX = user.MapX;
                    couple.MapY = user.MapY;

                    await couple.ProcessAfterMoveAsync();

                    MsgSyncAction msg = new MsgSyncAction
                    {
                        Action = SyncAction.Walk,
                        X = user.MapX,
                        Y = user.MapY
                    };
                    msg.Targets.Add(user.Identity);
                    msg.Targets.Add(couple.Identity);

                    await user.SendAsync(this);
                    await Kernel.BroadcastWorldMsgAsync(this);
                    Identity = couple.Identity;
                    await couple.SendAsync(this);
                    await Kernel.BroadcastWorldMsgAsync(this);

                    await user.SendAsync(msg);
                    await user.Screen.UpdateAsync(msg);
                    await couple.Screen.UpdateAsync();
                }
                else if (moved)
                {
                    await user.SendAsync(this);
                    await user.Screen.UpdateAsync(this);
                    await Kernel.BroadcastWorldMsgAsync(this);
                }
                return;
            }

            Role target = RoleManager.GetRole(Identity);
            if (target == null)
                return;

            await target.ProcessOnMoveAsync();
            await target.MoveTowardAsync(Direction, Mode);
            if (target.Screen != null)
            {
                await target.Screen.UpdateAsync(this);
            }
            else
            {
                await target.BroadcastRoomMsgAsync(this, false);
            }
            await Kernel.BroadcastWorldMsgAsync(this);
        }
    }
}