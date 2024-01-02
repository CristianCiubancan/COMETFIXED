using System.Threading.Tasks;
using Comet.AI.States;
using Comet.AI.World.Managers;
using Comet.Network.Packets.Ai;

namespace Comet.AI.Packets
{
    public sealed class MsgAiRoleStatusFlag : MsgAiRoleStatusFlag<Server>
    {
        public override async Task ProcessAsync(Server client)
        {
            Role target = RoleManager.GetRole(Identity);
            if (target == null)
                return;

            Role sender = RoleManager.GetRole(Caster);
            if (Mode == 0)
                await target.AttachStatusAsync(sender, Flag, 0, Duration, Steps, 0);
            else
                await target.DetachStatusAsync(Flag);
        }
    }
}