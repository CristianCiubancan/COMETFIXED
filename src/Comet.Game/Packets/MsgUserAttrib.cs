using Comet.Game.States;
using Comet.Network.Packets.Game;

namespace Comet.Game.Packets
{
    public sealed class MsgUserAttrib : MsgUserAttrib<Client>
    {
        public MsgUserAttrib(uint idRole, ClientUpdateType type, ulong value) : base(idRole, type, value)
        {
        }

        public MsgUserAttrib(uint idRole, ClientUpdateType type, uint value0, uint value1) : base(
            idRole, type, value0, value1)
        {
        }
    }
}