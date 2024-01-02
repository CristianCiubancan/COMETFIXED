using Comet.Game.States;
using Comet.Network.Packets;
using Comet.Network.Packets.Game;

namespace Comet.Game.Packets
{
    public sealed class MsgTradeBuddyInfo : MsgTradeBuddyInfo<Client>
    {
        public override byte[] Encode()
        {
            var writer = new PacketWriter();
            writer.Write((ushort) PacketType.MsgTradeBuffyInfo);
            writer.Write(Identity);
            writer.Write(Lookface);
            writer.Write(Level);
            writer.Write(Profession);
            writer.Write(PkPoints);
            writer.Write(Syndicate);
            writer.Write(SyndicatePosition);
            writer.Write(Unknown);
            writer.Write(Name, 16);
            return writer.ToArray();
        }
    }
}