namespace Comet.Network.Packets.Game
{
    public abstract class MsgGuideContribute<T> : MsgBase<T>
    {
        public ushort Composing;
        public uint Experience;
        public ushort HeavenBlessing;
        public uint Identity;

        public RequestType Mode;
        public byte[] Padding = new byte[12];
        public ushort Test1;
        public ushort Test2;

        public override void Decode(byte[] bytes)
        {
            var reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType) reader.ReadUInt16();
            Mode = (RequestType) reader.ReadUInt32();
            Identity = reader.ReadUInt32();
            Padding = reader.ReadBytes(12);
            Experience = reader.ReadUInt32();
            HeavenBlessing = reader.ReadUInt16();
            Composing = reader.ReadUInt16();
            Test1 = reader.ReadUInt16();
            Test2 = reader.ReadUInt16();
        }

        public override byte[] Encode()
        {
            var writer = new PacketWriter();
            writer.Write((ushort) PacketType.MsgGuideContribute);
            writer.Write((int) Mode);
            writer.Write(Identity);
            writer.Write(Padding);
            writer.Write(Experience);
            writer.Write(Test1);
            writer.Write(Test2);
            writer.Write(HeavenBlessing);
            writer.Write(Composing);
            return writer.ToArray();
        }

        public enum RequestType
        {
            ClaimExperience = 1,
            ClaimHeavenBlessing = 2,
            ClaimItemAdd = 3,
            Query = 4
        }
    }
}