using System.Threading.Tasks;
using Comet.Game.Internal.AI;
using Comet.Game.States;
using Comet.Game.World.Managers;
using Comet.Network.Packets;
using Comet.Network.Packets.Game;
using Comet.Shared;

namespace Comet.Game.Packets.Ai
{
    public sealed class MsgInteract : MsgInteract<AiClient>
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
            Role sender = RoleManager.GetRole(SenderIdentity);
            if (sender == null)
                return;

            if (sender.QueryStatus(StatusSet.FREEZE) != null
                || sender.QueryStatus(StatusSet.ICE_BLOCK) != null
                || sender.QueryStatus(StatusSet.DAZED) != null
                || sender.QueryStatus(StatusSet.HUGE_DAZED) != null
                || sender.QueryStatus(StatusSet.CONFUSED) != null)
                return;

            Role target = RoleManager.GetRole(TargetIdentity);

            switch (Action)
            {
                case MsgInteractType.Attack:
                case MsgInteractType.Shoot:
                {
                    if (!sender.IsAlive)
                        return;

                    if (sender.SetAttackTarget(target))
                        sender.BattleSystem.CreateBattle(TargetIdentity);
                    break;
                }

                case MsgInteractType.MagicAttack:
                {
                    if (!sender.IsAlive)
                        return;

                    await sender.ProcessMagicAttackAsync((ushort) Data, TargetIdentity, PosX, PosY);
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