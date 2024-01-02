using System.Collections.Generic;

namespace Comet.Network.Packets.Game
{
    public abstract class MsgQualifyingRank<T> : MsgBase<T>
    {
        public List<PlayerDataStruct> Players = new();
        public QueryRankType RankType { get; set; }
        public ushort PageNumber { get; set; }
        public int RankingNum { get; set; }
        public int Count { get; set; }

        public override void Decode(byte[] bytes)
        {
            var reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType) reader.ReadUInt16();
            RankType = (QueryRankType) reader.ReadUInt16();
            PageNumber = reader.ReadUInt16();
            RankingNum = reader.ReadInt32();
            Count = reader.ReadInt32();
        }

        public override byte[] Encode()
        {
            var writer = new PacketWriter();
            writer.Write((ushort) PacketType.MsgQualifyingRank);
            writer.Write((ushort) RankType);
            writer.Write(PageNumber);
            writer.Write(RankingNum);
            writer.Write(Count = Players.Count);
            foreach (PlayerDataStruct player in Players)
            {
                writer.Write(player.Rank);
                writer.Write(player.Name, 16);
                writer.Write(player.Type);
                writer.Write(player.Points);
                writer.Write(player.Profession);
                writer.Write(player.Level);
                // writer.Write(player.Unknown);
            }

            return writer.ToArray();
        }

        public struct PlayerDataStruct
        {
            public ushort Rank;
            public string Name;
            public ushort Type;
            public uint Points;
            public int Profession;
            public int Level;
            public int Unknown;
        }

        public enum QueryRankType : ushort
        {
            QualifierRank,
            HonorHistory
        }
    }
}