namespace Comet.Network.Packets.Ai
{
    public abstract class MsgAiAction<T> : MsgBase<T>
    {
        public AiAction Action { get; set; }
        public int Data { get; set; }
        public int Param { get; set; }
        public int Command { get; set; }
        public ushort X { get; set; }
        public ushort Y { get; set; }

        public override void Decode(byte[] bytes)
        {
            PacketReader reader = new(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType) reader.ReadUInt16();
            Action = (AiAction) reader.ReadInt32();
            Data = reader.ReadInt32();
            Param = reader.ReadInt32();
            Command = reader.ReadInt32();
            X = reader.ReadUInt16();
            Y = reader.ReadUInt16();
        }

        public override byte[] Encode()
        {
            PacketWriter writer = new();
            writer.Write((ushort) PacketType.MsgAiAction);
            writer.Write((int) Action);
            writer.Write(Data);
            writer.Write(Param);
            writer.Write(Command);
            writer.Write(X);
            writer.Write(Y);
            return writer.ToArray();
        }

        public enum AiAction
        {
            RequestLogin,
            Jump,
            Walk,
            FlyMap,
            AddTerrainObj,
            DelTerrainObj,
            SetProtection,
            ClearProtection,
            QueryRole
        }
    }
}