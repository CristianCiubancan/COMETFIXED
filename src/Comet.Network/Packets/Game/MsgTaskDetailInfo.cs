namespace Comet.Network.Packets.Game
{
    public abstract class MsgTaskDetailInfo<T> : MsgBase<T>
    {
        public uint Identity { get; set; }
        public uint TaskIdentity { get; set; }
        public int Data0 { get; set; }
        public int Data1 { get; set; }
        public int Data2 { get; set; }
        public int Data3 { get; set; }
        public int Data4 { get; set; }
        public int Data5 { get; set; }
        public int Data6 { get; set; }
        public int Data7 { get; set; }

        public override void Decode(byte[] bytes)
        {
            var reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType) reader.ReadUInt16();
            Identity = reader.ReadUInt32();
            TaskIdentity = reader.ReadUInt32();
            Data0 = reader.ReadInt32();
            Data1 = reader.ReadInt32();
            Data2 = reader.ReadInt32();
            Data3 = reader.ReadInt32();
            Data4 = reader.ReadInt32();
            Data5 = reader.ReadInt32();
            Data6 = reader.ReadInt32();
            Data7 = reader.ReadInt32();
        }

        public override byte[] Encode()
        {
            var writer = new PacketWriter();
            writer.Write((ushort) PacketType.MsgTaskDetailInfo);
            writer.Write(Identity);
            writer.Write(TaskIdentity);
            writer.Write(Data0);
            writer.Write(Data1);
            writer.Write(Data2);
            writer.Write(Data3);
            writer.Write(Data4);
            writer.Write(Data5);
            writer.Write(Data6);
            writer.Write(Data7);
            return writer.ToArray();
        }
    }
}