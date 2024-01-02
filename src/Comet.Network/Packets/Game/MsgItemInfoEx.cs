namespace Comet.Network.Packets.Game
{
    public abstract class MsgItemInfoEx<T> : MsgBase<T>
    {
        public byte Addition;
        public ushort Amount;
        public ushort AmountLimit;
        public byte Blessing;
        public byte Color;
        public uint CompositionProgress;
        public byte Enchantment;

        public uint Identity;
        public uint ItemType;
        public ViewMode Mode;
        public ushort Position;
        public uint Price;
        public byte SocketOne;
        public uint SocketProgress;
        public byte SocketTwo;
        public uint TargetIdentity;

        public override byte[] Encode()
        {
            var writer = new PacketWriter();
            writer.Write((ushort) PacketType.MsgItemInfoEx);
            writer.Write(Identity);
            writer.Write(TargetIdentity);
            writer.Write(Price);
            writer.Write(ItemType);
            writer.Write(Amount);
            writer.Write(AmountLimit);
            writer.Write((ushort) Mode);
            writer.Write(Position);
            writer.Write(SocketProgress);
            writer.Write(SocketOne);
            writer.Write(SocketTwo);
            writer.Write((ushort) 0);
            writer.Write(Addition);
            writer.Write(Blessing);
            writer.Write(Enchantment);
            writer.Write((byte) 0);
            writer.Write(0UL);
            writer.Write((ushort) Color);
            writer.Write(CompositionProgress);
            return writer.ToArray();
        }

        public enum ViewMode : ushort
        {
            None,
            Silvers,
            Unknown,
            Emoney,
            ViewEquipment
        }
    }
}