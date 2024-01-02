using System.Collections.Generic;

namespace Comet.Network.Packets.Game
{
    public abstract class MsgFactionRankInfo<T> : MsgBase<T>
    {
        public List<MemberListInfoStruct> Members = new();

        public RankRequestType DonationType { get; set; }
        public ushort Count { get; set; }
        public ushort MaxCount { get; set; } = MAX_COUNT;

        public override void Decode(byte[] bytes)
        {
            var reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType) reader.ReadUInt16();
            DonationType = (RankRequestType) reader.ReadUInt16();
            Count = reader.ReadUInt16();
            MaxCount = reader.ReadUInt16();
        }

        public override byte[] Encode()
        {
            var writer = new PacketWriter();
            writer.Write((ushort) PacketType.MsgFactionRankInfo);
            writer.Write((ushort) DonationType);
            writer.Write(Count = (ushort) Members.Count);
            writer.Write((int) MAX_COUNT);
            writer.Write(0);
            foreach (MemberListInfoStruct member in Members)
            {
                //writer.Write(member.PlayerIdentity); // 0
                writer.Write(member.Rank);            // 4
                writer.Write(member.Position);        // 8
                writer.Write(member.Silvers);         // 12
                writer.Write(member.ConquerPoints);   // 16
                writer.Write(member.PkDonation);      // 20
                writer.Write(member.GuideDonation);   // 24
                writer.Write(member.ArsenalDonation); // 28
                writer.Write(member.RedRose);         // 32
                writer.Write(member.WhiteRose);       // 36
                writer.Write(member.Orchid);          // 40
                writer.Write(member.Tulip);           // 44
                writer.Write(member.TotalDonation);   // 48
                writer.Write(member.UsableDonation);
                writer.Write(0);
                writer.Write(0);
                writer.Write(0);
                writer.Write(0);
                writer.Write(member.PlayerName, 16); // 52
                writer.Write(0);
            }

            return writer.ToArray();
        }

        public struct MemberListInfoStruct
        {
            public uint PlayerIdentity { get; set; }
            public int Rank { get; set; }
            public int Position { get; set; }
            public int Silvers { get; set; }
            public uint ConquerPoints { get; set; }
            public int PkDonation { get; set; }
            public uint GuideDonation { get; set; }
            public uint ArsenalDonation { get; set; }
            public uint RedRose { get; set; }
            public uint WhiteRose { get; set; }
            public uint Orchid { get; set; }
            public uint Tulip { get; set; }
            public uint TotalDonation { get; set; }
            public int UsableDonation { get; set; }
            public string PlayerName { get; set; }
        }

        public const ushort MAX_COUNT = 10;

        public enum RankRequestType
        {
            Silvers,
            ConquerPoints,
            Guide,
            PK,
            Arsenal,
            RedRose,
            Orchid,
            WhiteRose,
            Tulip,
            Usable,
            Total
        }
    }
}