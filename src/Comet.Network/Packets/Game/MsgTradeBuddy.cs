namespace Comet.Network.Packets.Game
{
    public abstract class MsgTradeBuddy<T> : MsgBase<T>
    {
        public TradeBuddyAction Action;
        public int HoursLeft;

        public uint Identity;
        public bool IsOnline;
        public string Name;

        public override void Decode(byte[] bytes)
        {
            var reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType) reader.ReadUInt16();
            Identity = reader.ReadUInt32();
            Action = (TradeBuddyAction) reader.ReadByte();
            IsOnline = reader.ReadBoolean();
            HoursLeft = reader.ReadInt32();
            reader.ReadUInt16();
            Name = reader.ReadString(16);
        }

        public override byte[] Encode()
        {
            var writer = new PacketWriter();
            writer.Write((ushort) PacketType.MsgTradeBuddy);
            writer.Write(Identity);
            writer.Write((byte) Action);
            writer.Write(IsOnline);
            writer.Write(HoursLeft);
            writer.Write((ushort) 0);
            writer.Write(Name, 16);
            return writer.ToArray();
        }

        public enum TradeBuddyAction
        {
            RequestPartnership = 0,
            RejectRequest = 1,
            BreakPartnership = 4,
            AddPartner = 5
        }
    }
}