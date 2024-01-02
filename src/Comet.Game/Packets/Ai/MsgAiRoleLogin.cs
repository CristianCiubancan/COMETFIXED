using Comet.Game.Internal.AI;
using Comet.Game.States;
using Comet.Network.Packets.Ai;

namespace Comet.Game.Packets.Ai
{
    public sealed class MsgAiRoleLogin : MsgAiRoleLogin<AiClient>
    {
        public MsgAiRoleLogin()
        {
            
        }

        public MsgAiRoleLogin(Monster monster)
        {
            NpcType = monster.IsCallPet() ? RoleLoginNpcType.CallPet : RoleLoginNpcType.Monster;
            Generator = (int) (monster.IsCallPet() ? 0 : monster.GeneratorId);
            Identity = monster.Identity;
            Name = monster.Name;
            LookFace = (int) monster.Type;
            MapId = monster.MapIdentity;
            MapX = monster.MapX;
            MapY = monster.MapY;
        }
    }
}
