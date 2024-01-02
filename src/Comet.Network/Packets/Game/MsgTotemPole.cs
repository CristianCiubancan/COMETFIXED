namespace Comet.Network.Packets.Game
{
    public abstract class MsgTotemPole<T> : MsgBase<T>
    {
        public ActionMode Action { get; set; }
        public int Data1 { get; set; }
        public int Data2 { get; set; }
        public int Data3 { get; set; }
        public int Unknown20 { get; set; }
        public int Unknown24 { get; set; }


        public override void Decode(byte[] bytes)
        {
            var reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType) reader.ReadUInt16();
            Action = (ActionMode) reader.ReadUInt32();
            Data1 = reader.ReadInt32();
            Data2 = reader.ReadInt32();
            Data3 = reader.ReadInt32();
            Unknown20 = reader.ReadInt32();
            Unknown24 = reader.ReadInt32();
        }

        public override byte[] Encode()
        {
            var writer = new PacketWriter();
            writer.Write((ushort) PacketType.MsgTotemPole);
            writer.Write((uint) Action);
            writer.Write(Data1);
            writer.Write(Data2);
            writer.Write(Data3);
            writer.Write(Unknown20);
            writer.Write(Unknown24);
            return writer.ToArray();
        }

        public enum ActionMode
        {
            UnlockArsenal,
            InscribeItem,
            UnsubscribeItem,
            Enhance,
            Refresh
        }
    }
}