namespace Comet.Network.Packets.Game
{
    public abstract class MsgEquipLock<T> : MsgBase<T>
    {
        public uint Identity { get; set; }
        public LockMode Action { get; set; }
        public byte Mode { get; set; }
        public uint Param { get; set; }

        public override void Decode(byte[] bytes)
        {
            var reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType) reader.ReadUInt16();
            Identity = reader.ReadUInt32();
            Action = (LockMode) reader.ReadByte();
            Mode = reader.ReadByte();
            reader.ReadUInt16();
            Param = reader.ReadUInt32();
        }

        public override byte[] Encode()
        {
            var writer = new PacketWriter();
            writer.Write((ushort) PacketType.MsgEquipLock);
            writer.Write(Identity);
            writer.Write((byte) Action);
            writer.Write(Mode);
            writer.Write((ushort) 0);
            writer.Write(Param);
            return writer.ToArray();
        }

        public enum LockMode : byte
        {
            RequestLock = 0,
            RequestUnlock = 1,
            UnlockDate = 2,
            UnlockedItem = 3
        }
    }
}