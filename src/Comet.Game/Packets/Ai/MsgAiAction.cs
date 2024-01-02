using System.Threading.Tasks;
using Comet.Game.Internal.AI;
using Comet.Game.States;
using Comet.Game.World.Managers;
using Comet.Network.Packets.Ai;

namespace Comet.Game.Packets.Ai
{
    public sealed class MsgAiAction : MsgAiAction<AiClient>
    {
        public override async Task ProcessAsync(AiClient client)
        {
            switch (Action)
            {
                case AiAction.QueryRole:
                {
                    Role role = RoleManager.GetRole((uint) Data);
                    if (role == null)
                    {
                        // show message? much spam?
                        return;
                    }

                    if (role.IsPlayer())
                    {
                        await client.SendAsync(new MsgAiPlayerLogin(role as Character));
                    }
                    else if (role.IsCallPet())
                    {

                    }

                    break;
                }
            }
        }
    }
}