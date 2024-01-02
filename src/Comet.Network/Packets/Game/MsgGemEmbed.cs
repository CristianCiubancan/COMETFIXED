namespace Comet.Network.Packets.Game
{
    public abstract class MsgGemEmbed<T> : MsgBase<T>
    {
        public uint Identity { get; set; }
        public uint MainIdentity { get; set; }
        public uint MinorIdentity { get; set; }
        public ushort Position { get; set; }
        public EmbedAction Action { get; set; }

        public override void Decode(byte[] bytes)
        {
            var reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType) reader.ReadUInt16();
            Identity = reader.ReadUInt32();
            MainIdentity = reader.ReadUInt32();
            MinorIdentity = reader.ReadUInt32();
            Position = reader.ReadUInt16();
            Action = (EmbedAction) reader.ReadUInt16();
        }

        public override byte[] Encode()
        {
            var writer = new PacketWriter();
            writer.Write((ushort) PacketType.MsgGemEmbed);
            writer.Write(Identity);
            writer.Write(MainIdentity);
            writer.Write(MinorIdentity);
            writer.Write(Position);
            writer.Write((ushort) Action);
            return writer.ToArray();
        }

        public enum EmbedAction : ushort
        {
            Embed = 0,
            TakeOff = 1
        }
    }
}