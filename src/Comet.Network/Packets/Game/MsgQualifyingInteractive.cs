namespace Comet.Network.Packets.Game
{
    public abstract class MsgQualifyingInteractive<T> : MsgBase<T>
    {
        public InteractionType Interaction { get; set; }
        public int Option { get; set; }
        public uint Identity { get; set; }
        public string Name { get; set; } = "";
        public int Rank { get; set; }
        public int Profession { get; set; }
        public int Unknown40 { get; set; }
        public int Points { get; set; }
        public int Level { get; set; }

        public override void Decode(byte[] bytes)
        {
            var reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType) reader.ReadUInt16();
            Interaction = (InteractionType) reader.ReadInt32();
            Option = reader.ReadInt32();
            Identity = reader.ReadUInt32();
            Name = reader.ReadString(16);
            Rank = reader.ReadInt32();
            Profession = reader.ReadInt32();
            Unknown40 = reader.ReadInt32();
            Points = reader.ReadInt32();
            Level = reader.ReadInt32();
        }

        public override byte[] Encode()
        {
            var writer = new PacketWriter();
            writer.Write((ushort) PacketType.MsgQualifyingInteractive);
            writer.Write((int) Interaction);
            writer.Write(Option);
            writer.Write(Identity);
            writer.Write(Name, 16);
            writer.Write(Rank);
            writer.Write(Profession);
            writer.Write(Unknown40);
            writer.Write(Points);
            writer.Write(Level);
            return writer.ToArray();
        }

        public enum InteractionType
        {
            Inscribe,
            Unsubscribe,
            Countdown,
            Accept,
            GiveUp,
            BuyArenaPoints,
            Match,
            YouAreKicked,
            StartTheFight,
            Dialog,

            //EndDialog,
            ReJoin
        }
    }
}