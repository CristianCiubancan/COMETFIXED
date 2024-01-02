using System.Collections.Generic;

namespace Comet.Network.Packets.Game
{
    public abstract class MsgDutyMinContri<T> : MsgBase<T>
    {
        public List<MinContriStruct> Members = new();
        public ushort Action { get; set; }
        public ushort Count { get; set; }

        public override byte[] Encode()
        {
            var writer = new PacketWriter();
            writer.Write((ushort) PacketType.MsgDutyMinContri);
            writer.Write(Action);
            writer.Write(Count = (ushort) Members.Count);
            foreach (MinContriStruct member in Members)
            {
                writer.Write(member.Position);
                writer.Write(member.Donation);
            }

            return writer.ToArray();
        }

        public struct MinContriStruct
        {
            public int Position;
            public uint Donation;
        }
    }
}