using System.Threading.Tasks;
using Comet.Game.States;
using Comet.Game.States.Npcs;
using Comet.Game.World.Managers;
using Comet.Network.Packets.Game;

namespace Comet.Game.Packets
{
    public sealed class MsgNpc : MsgNpc<Client>
    {
        public override async Task ProcessAsync(Client client)
        {
            Character user = client.Character;

            switch (RequestType)
            {
                case NpcActionType.Activate:
                {
                    user.ClearTaskId();
                    Role role = RoleManager.GetRole(Identity);
                    if (role is BaseNpc npc
                        && (role.MapIdentity == user.MapIdentity
                            && role.GetDistance(user) <= 18
                            || role.MapIdentity == 5000))
                    {
                        user.InteractingNpc = npc.Identity;
                        await npc.ActivateNpc(user);
                    }

                    break;
                }

                case NpcActionType.CancelInteraction:
                {
                    user.CancelInteraction();
                    break;
                }
            }
        }
    }
}