namespace Comet.Network.Packets.Game
{
    public abstract class MsgArenicScore<T> : MsgBase<T>
    {
        public uint Identity1 { get; set; }
        public string Name1 { get; set; }
        public int Damage1 { get; set; }

        public uint Identity2 { get; set; }
        public string Name2 { get; set; }
        public int Damage2 { get; set; }

        public override byte[] Encode()
        {
            var writer = new PacketWriter();
            writer.Write((ushort) PacketType.MsgArenicScore);
            writer.Write(Identity1);
            writer.Write(Name1, 16);
            writer.Write(Damage1);
            writer.Write(Identity2);
            writer.Write(Name2, 16);
            writer.Write(Damage2);
            return writer.ToArray();
        }
    }
}