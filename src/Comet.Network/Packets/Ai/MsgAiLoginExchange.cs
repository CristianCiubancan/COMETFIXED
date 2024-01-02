namespace Comet.Network.Packets.Ai
{
    public abstract class MsgAiLoginExchange<T> : MsgBase<T>
    {
        public string UserName { get; set; } = "";
        public string Password { get; set; } = "";
        public string ServerName { get; set; } = "";

        public override void Decode(byte[] bytes)
        {
            PacketReader reader = new(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType) reader.ReadUInt16();
            UserName = reader.ReadString(16);
            Password = reader.ReadString(16);
            ServerName = reader.ReadString(16);
        }

        public override byte[] Encode()
        {
            PacketWriter writer = new();
            writer.Write((ushort) PacketType.MsgAiLoginExchange);
            writer.Write(UserName, 16);
            writer.Write(Password, 16);
            writer.Write(ServerName, 16);
            return writer.ToArray();
        }
    }
}