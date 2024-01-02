using Comet.Game.States;
using Comet.Network.Packets.Game;

namespace Comet.Game.Packets
{
    public sealed class MsgMapInfo : MsgMapInfo<Client>
    {
        public MsgMapInfo(uint mapId, uint mapDoc, ulong flags)
            : base(mapId, mapDoc, flags)
        {
        }
    }
}