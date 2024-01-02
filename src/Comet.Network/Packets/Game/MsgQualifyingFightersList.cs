using System.Collections.Generic;

namespace Comet.Network.Packets.Game
{
    public abstract class MsgQualifyingFightersList<T> : MsgBase<T>
    {
        public int Page { get; set; }
        public int Unknown8 { get; set; }
        public int MatchesCount { get; set; }
        public int FightersNum { get; set; }
        public int Unknown20 { get; set; }
        public int Count { get; set; }
        public List<FightStruct> Fights { get; set; } = new();

        public override void Decode(byte[] bytes)
        {
            var reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType) reader.ReadUInt16();
            Page = reader.ReadInt32();         // 4
            Unknown8 = reader.ReadInt32();     // 8
            MatchesCount = reader.ReadInt32(); // 12
            FightersNum = reader.ReadInt32();  // 16
            Unknown20 = reader.ReadInt32();    // 20
            Count = reader.ReadInt32();        // 24
        }

        public override byte[] Encode()
        {
            var writer = new PacketWriter();
            writer.Write((ushort) PacketType.MsgQualifyingFightersList);
            writer.Write(Page);
            writer.Write(Unknown8);
            writer.Write(MatchesCount = Fights.Count);
            writer.Write(FightersNum);
            writer.Write(Unknown20);
            writer.Write(Count = Fights.Count);
            foreach (FightStruct fight in Fights)
            {
                writer.Write(fight.Fighter0.Identity);
                writer.Write(fight.Fighter0.Mesh);
                writer.Write(fight.Fighter0.Name, 16);
                writer.Write(fight.Fighter0.Level);
                writer.Write(fight.Fighter0.Profession);
                //writer.Write(fight.Fighter0.Unknown);
                writer.Write(fight.Fighter0.Rank);
                writer.Write(fight.Fighter0.Points);
                writer.Write(fight.Fighter0.WinsToday);
                writer.Write(fight.Fighter0.LossToday);
                writer.Write(fight.Fighter0.CurrentHonor);
                writer.Write(fight.Fighter0.TotalHonor);

                writer.Write(fight.Fighter1.Identity);
                writer.Write(fight.Fighter1.Mesh);
                writer.Write(fight.Fighter1.Name, 16);
                writer.Write(fight.Fighter1.Level);
                writer.Write(fight.Fighter1.Profession);
                //writer.Write(fight.Fighter1.Unknown);
                writer.Write(fight.Fighter1.Rank);
                writer.Write(fight.Fighter1.Points);
                writer.Write(fight.Fighter1.WinsToday);
                writer.Write(fight.Fighter1.LossToday);
                writer.Write(fight.Fighter1.CurrentHonor);
                writer.Write(fight.Fighter1.TotalHonor);
            }

            return writer.ToArray();
        }

        public struct FightStruct
        {
            public FighterInfoStruct Fighter0;
            public FighterInfoStruct Fighter1;
        }

        public struct FighterInfoStruct
        {
            public uint Identity;    // 0
            public uint Mesh;        // 4
            public string Name;      // 8
            public int Level;        // 24
            public int Profession;   // 28
            public int Unknown;      // 32
            public int Rank;         // 36
            public int Points;       // 40
            public int WinsToday;    // 44
            public int LossToday;    // 48
            public int CurrentHonor; // 52
            public int TotalHonor;   // 56
        }
    }
}