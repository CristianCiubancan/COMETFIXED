namespace Comet.Network.Packets.Game
{
    public abstract class MsgInviteTrans<T> : MsgBase<T>
    {
        public Action Mode { get; set; }
        public int Message { get; set; }
        public int Priority { get; set; }
        public int Seconds { get; set; }

        public override void Decode(byte[] bytes)
        {
            var reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType) reader.ReadUInt16();
            Mode = (Action) reader.ReadInt32();
            Message = reader.ReadInt32();
            Priority = reader.ReadInt32();
            Seconds = reader.ReadInt32();
        }

        public override byte[] Encode()
        {
            PacketWriter writer = new();
            writer.Write((ushort) PacketType.MsgInviteTrans);
            writer.Write((int) Mode);
            writer.Write(Message);
            writer.Write(Priority);
            writer.Write(Seconds);
            return writer.ToArray();
        }

        public enum Action
        {
            Pop,
            Accept,
            AcceptMessage
        }
    }
}