namespace Comet.Network.Packets.Game
{
    public abstract class MsgSuitStatus<T> : MsgBase<T>
    {
        public int Action { get; set; }
        public int Unknown { get; set; }
        public int Data { get; set; }
        public int Param { get; set; }

        public override void Decode(byte[] bytes)
        {
            var reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType) reader.ReadUInt16();
            Action = reader.ReadInt32();
            Unknown = reader.ReadInt32();
            Data = reader.ReadInt32();
            Param = reader.ReadInt32();
        }

        public override byte[] Encode()
        {
            var writer = new PacketWriter();
            writer.Write((ushort) PacketType.MsgSuitStatus);
            writer.Write(Action);
            writer.Write(Unknown);
            writer.Write(Data);
            writer.Write(Param);
            return writer.ToArray();
        }
    }
}