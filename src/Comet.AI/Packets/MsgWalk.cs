using System.Threading.Tasks;
using Comet.AI.States;
using Comet.AI.World.Managers;
using Comet.Network.Packets.Ai;
using Comet.Network.Packets.Game;

namespace Comet.AI.Packets
{
    public sealed class MsgWalk : MsgWalk<Server>
    {
        public override async Task ProcessAsync(Server client)
        {
            Role target = RoleManager.GetRole(Identity);
            if (target == null)
            {
                if (Role.IsPlayer(Identity) || Role.IsCallPet(Identity))
                {
                    await client.SendAsync(new MsgAiAction
                    {
                        Action = MsgAiAction<Server>.AiAction.QueryRole,
                        Data = (int) Identity
                    });
                }
                return;
            }

            await target.MoveTowardAsync(Direction, Mode, false);
        }
    }
}