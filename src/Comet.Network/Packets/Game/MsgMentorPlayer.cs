namespace Comet.Network.Packets.Game
{
    public abstract class MsgMentorPlayer<T> : MsgBase<T>
    {
        public int Timestamp { get; set; }
        public int Action { get; set; }
        public uint SenderId { get; set; }
        public uint TargetId { get; set; }
        public int Unknown2 { get; set; }
        public int Unknown3 { get; set; }
        public int Unknown4 { get; set; }

        public override void Decode(byte[] bytes)
        {
            var reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType) reader.ReadUInt16();
            Action = reader.ReadInt32();    // 4
            SenderId = reader.ReadUInt32(); // 8 
            TargetId = reader.ReadUInt32(); // 12
            Unknown2 = reader.ReadInt32();  // 16
            Unknown3 = reader.ReadInt32();  // 20
            Unknown4 = reader.ReadInt32();  // 24
        }

        public override byte[] Encode()
        {
            var writer = new PacketWriter();
            writer.Write((ushort) PacketType.MsgMentorPlayer);
            writer.Write(Action);
            writer.Write(SenderId);
            writer.Write(TargetId);
            writer.Write(Unknown2);
            writer.Write(Unknown3);
            writer.Write(Unknown4);
            return writer.ToArray();
        }
    }
}