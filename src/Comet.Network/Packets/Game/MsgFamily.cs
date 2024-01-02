using System.Collections.Generic;

namespace Comet.Network.Packets.Game
{
    public abstract class MsgFamily<T> : MsgBase<T>
    {
        public FamilyAction Action;
        public uint Identity;
        public List<object> Objects = new();
        public List<string> Strings = new();
        public uint Unknown;

        public override void Decode(byte[] bytes)
        {
            var reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType) reader.ReadUInt16();
            Action = (FamilyAction) reader.ReadUInt32();
            Identity = reader.ReadUInt32();
            Unknown = reader.ReadUInt32();
            Strings = reader.ReadStrings();
        }

        public override byte[] Encode()
        {
            var writer = new PacketWriter();
            writer.Write((ushort) PacketType.MsgFamily);
            writer.Write((int) Action);
            writer.Write(Identity);
            writer.Write(Unknown);
            if (Objects.Count == 0)
            {
                writer.Write(Strings);
            }
            else
            {
                var idx = 0;
                writer.Write(Objects.Count);
                foreach (object obj in Objects)
                {
                    if (obj is MemberListStruct member)
                    {
                        writer.Write(member.Name, 16);
                        writer.Write((int) member.Level);
                        writer.Write(member.Rank);
                        writer.Write((ushort) (member.Online ? 1 : 0));
                        writer.Write((int) member.Profession);
                        writer.Write(member.Donation);
                    }
                    else if (obj is RelationListStruct relation)
                    {
                        writer.Write(idx + 101);
                        writer.Write(relation.Name, 16);
                        writer.Write(0);
                        writer.Write(0);
                        writer.Write(0);
                        writer.Write(0);
                        writer.Write(0);
                        writer.Write(relation.LeaderName, 16);
                    }

                    idx++;
                }
            }

            return writer.ToArray();
        }

        public struct MemberListStruct
        {
            public string Name;
            public byte Level;
            public ushort Rank;
            public bool Online;
            public ushort Profession;
            public uint Donation;
        }

        public struct RelationListStruct
        {
            public string Name;
            public string LeaderName;
        }

        public enum FamilyAction
        {
            Query = 1,
            QueryMemberList = 4,
            Recruit = 9,
            AcceptRecruit = 10,
            Join = 11,
            AcceptJoinRequest = 12,
            SendEnemy = 13,
            AddEnemy = 14,
            DeleteEnemy = 15,
            SendAlly = 16,
            AddAlly = 17,
            AcceptAlliance = 18,
            DeleteAlly = 20,
            Abdicate = 21,
            KickOut = 22,
            Quit = 23,
            Announce = 24,
            SetAnnouncement = 25,
            Dedicate = 26,
            QueryOccupy = 29
        }
    }
}