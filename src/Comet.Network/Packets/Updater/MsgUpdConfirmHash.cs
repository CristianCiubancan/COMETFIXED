namespace Comet.Network.Packets.Updater
{
    public abstract class MsgUpdConfirmHash<T> : MsgBase<T>
    {
        public const int SUCCESS = 0x3F56FA;
        
        public int Result { get; set; }

        /// <inheritdoc />
        public override void Decode(byte[] bytes)
        {
            PacketReader reader = new(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType) reader.ReadUInt16();
            Result = reader.ReadInt32();
        }

        /// <inheritdoc />
        public override byte[] Encode()
        {
            PacketWriter writer = new PacketWriter();
            writer.Write((ushort) PacketType.MsgUpdConfirmHash);
            writer.Write(Result);
            return writer.ToArray();
        }
    }
}
