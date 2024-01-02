using System.Collections.Generic;

namespace Comet.Network.Packets.Game
{
    public abstract class MsgSynMemberList<T> : MsgBase<T>
    {
        public uint SubType { get; set; }
        public int Index { get; set; }
        public int Amount { get; set; }
        public List<MemberStruct> Members { get; set; } = new();

        public override void Decode(byte[] bytes)
        {
            var reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType) reader.ReadUInt16();
            SubType = reader.ReadUInt32();
            Index = reader.ReadInt32();
            Amount = reader.ReadInt32();
        }

        public override byte[] Encode()
        {
            var writer = new PacketWriter();
            writer.Write((ushort) PacketType.MsgSynMemberList);
            writer.Write(SubType);
            writer.Write(Index);
            writer.Write(Amount = Members.Count);
            foreach (MemberStruct member in Members)
            {
                writer.Write(member.Name, 16);
                writer.Write(0);
                writer.Write((int) (member.Nobility * 10 + member.LookFace % 10000 / 1000));
                writer.Write(member.Level);
                writer.Write(member.Rank);
                writer.Write(member.PositionExpire);
                writer.Write(member.TotalDonation);
                writer.Write(member.IsOnline ? 1 : 0);
                writer.Write(0); // test
            }

            return writer.ToArray();
        }

        public struct MemberStruct
        {
            public uint Identity { get; set; }
            public uint LookFace { get; set; }
            public string Name { get; set; }
            public int Level { get; set; }
            public int Nobility { get; set; }
            public int Rank { get; set; }
            public uint PositionExpire { get; set; }
            public int TotalDonation { get; set; }
            public bool IsOnline { get; set; }
        }
    }
}