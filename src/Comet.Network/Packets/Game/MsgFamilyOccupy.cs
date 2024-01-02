using System.IO;

namespace Comet.Network.Packets.Game
{
    public abstract class MsgFamilyOccupy<T> : MsgBase<T>
    {
        public FamilyPromptType Action; // 4
        public bool CanApplyChallenge;  // 93
        public bool CanClaimExperience;
        public bool CanClaimRevenue;
        public bool CanRemoveChallenge; // 94
        public string CityName;         // 56
        public uint DailyPrize;         // 100
        public uint GoldFee;            // 120
        public uint Identity;           // 8
        public uint IsChallenged;       // 112
        public uint OccupyDays;         // 96
        public string OccupyName;       // 20
        public uint RequestNpc;         // 12
        public uint SubAction;          // 16
        public bool UnknownBool3;       // 95
        public bool WarRunning;         // 92
        public uint WeeklyPrize;        // 104

        public override void Decode(byte[] bytes)
        {
            var reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType) reader.ReadUInt16();
            Action = (FamilyPromptType) reader.ReadInt32();
            Identity = reader.ReadUInt32();
            RequestNpc = reader.ReadUInt32();
            SubAction = reader.ReadUInt32();
            OccupyName = reader.ReadString(16);
            reader.BaseStream.Seek(20, SeekOrigin.Current);
            CityName = reader.ReadString(16);
            reader.BaseStream.Seek(24, SeekOrigin.Current);
            OccupyDays = reader.ReadUInt32();
            DailyPrize = reader.ReadUInt32();
            WeeklyPrize = reader.ReadUInt32();
            reader.BaseStream.Seek(12, SeekOrigin.Current);
            GoldFee = reader.ReadUInt32();
        }

        public override byte[] Encode()
        {
            var writer = new PacketWriter();
            writer.Write((ushort) (Type = PacketType.MsgFamilyOccupy));
            writer.Write((int) Action);
            writer.Write(Identity);
            writer.Write(RequestNpc);
            writer.Write(SubAction);
            writer.Write(OccupyName, 16);
            writer.BaseStream.Seek(20, SeekOrigin.Current);
            writer.Write(CityName, 16);
            writer.BaseStream.Seek(20, SeekOrigin.Current);
            writer.Write(WarRunning);
            writer.Write(CanApplyChallenge);
            writer.Write(CanRemoveChallenge);
            writer.Write(UnknownBool3);
            writer.Write(OccupyDays);
            writer.Write(DailyPrize);
            writer.Write(WeeklyPrize);
            writer.Write(0);
            writer.Write(IsChallenged); // Challenged by other clans
            writer.Write(0);
            writer.Write(GoldFee);
            writer.Write(0);
            writer.Write(CanClaimRevenue);    // allow claim
            writer.Write(CanClaimExperience); // claim exp
            writer.Write((ushort) 0);
            writer.Write(0);
            return writer.ToArray();
        }

        public enum FamilyPromptType
        {
            Challenge = 0, // Client -> Server 
            CancelChallenge = 1,
            AbandonMap = 2,
            RemoveChallenge = 3,
            ChallengeMap = 4,
            Unknown5 = 5,          // Probably Client -> Server
            RequestNpc = 6,        // Npc Click Client -> Server -> Client
            AnnounceWarBegin = 7,  // Call to war Server -> Client
            AnnounceWarAccept = 8, // Answer Ok to annouce Client -> Server
            ClaimExperience = 10,
            WrongClaimTime = 13,    // Claim once a day
            CannotClaim = 12,       // New members cannot claim
            ClaimOnceADay = 14,     // Claimed
            ClaimedAlready = 15,    // Claimed, claim tomorrow
            WrongExpClaimTime = 16, // Claimed
            ReachedMaxLevel = 17,
            ClaimRevenue = 18
        }
    }
}