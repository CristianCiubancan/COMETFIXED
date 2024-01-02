namespace Comet.Network.Packets.Game
{
    public abstract class MsgTradeBuddyInfo<T> : MsgBase<T>
    {
        public uint Identity;
        public byte Level;
        public uint Lookface;
        public string Name;
        public ushort PkPoints;
        public byte Profession;
        public uint Syndicate;
        public int SyndicatePosition;
        public ushort Unknown;

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