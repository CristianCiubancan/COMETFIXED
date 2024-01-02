namespace Comet.Network.Packets.Game
{
    public abstract class MsgTrainingInfo<T> : MsgBase<T>
    {
        public ushort TimeUsed { get; set; }
        public ushort TimeRemaining { get; set; }
        public int Level { get; set; }
        public int Experience { get; set; }

        public override byte[] Encode()
        {
            var writer = new PacketWriter();
            writer.Write((ushort) PacketType.MsgTrainingInfo);
            writer.Write(TimeUsed);
            writer.Write(TimeRemaining);
            writer.Write(Level);
            writer.Write(Experience);
            return writer.ToArray();
        }
    }
}