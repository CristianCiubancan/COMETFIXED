namespace Comet.Network.Packets
{
    public abstract class MsgPetInfo<T> : MsgBase<T>
    {
        public uint Identity { get; set; }
        public uint LookFace { get; set; }
        public uint AiType { get; set; }
        public ushort X { get; set; }
        public ushort Y { get; set; }
        public string Name { get; set; }

        /// <inheritdoc />
        public override byte[] Encode()
        {
            PacketWriter writer = new();
            writer.Write((ushort) PacketType.MsgPetInfo);
            writer.Write(Identity);
            writer.Write(LookFace);
            writer.Write(AiType);
            writer.Write(X);
            writer.Write(Y);
            writer.Write(Name, 16);
            return writer.ToArray();
        }
    }
}