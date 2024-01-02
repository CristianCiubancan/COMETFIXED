using System.Collections.Generic;

namespace Comet.Network.Packets.Game
{
    public abstract class MsgGuideInfo<T> : MsgBase<T>
    {
        public ushort Blessing;
        public ushort Composition;
        public uint EnroleDate;
        public ulong Experience;
        public byte[] Fill41 = new byte[3];
        public uint Identity;
        public bool IsOnline;
        public byte Level;
        public uint Mesh;

        public RequestMode Mode;
        public List<string> Names = new();
        public ushort PkPoints;
        public byte Profession;
        public uint SenderIdentity;
        public uint SharedBattlePower;
        public ushort Syndicate;
        public ushort SyndicatePosition;
        public uint Unknown24;
        public byte Unknown38;
        public ulong Unknown40;
        public uint Unknown52;

        public override void Decode(byte[] bytes)
        {
            var reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType) reader.ReadUInt16();
            Mode = (RequestMode) reader.ReadUInt32();
            SenderIdentity = reader.ReadUInt32();
            Identity = reader.ReadUInt32();
            Mesh = reader.ReadUInt32();
            SharedBattlePower = reader.ReadUInt32();
            Unknown24 = reader.ReadUInt32();
            EnroleDate = reader.ReadUInt32();
            Level = reader.ReadByte();
            Profession = reader.ReadByte();
            PkPoints = reader.ReadUInt16();
            Syndicate = reader.ReadUInt16();
            Unknown38 = reader.ReadByte();
            SyndicatePosition = reader.ReadByte();
            Unknown40 = reader.ReadUInt64();
            IsOnline = reader.ReadBoolean();
            Fill41 = reader.ReadBytes(Fill41.Length);
            Unknown52 = reader.ReadUInt32();
            Experience = reader.ReadUInt64();
            Blessing = reader.ReadUInt16();
            Composition = reader.ReadUInt16();
            Names = reader.ReadStrings();
        }

        public override byte[] Encode()
        {
            var writer = new PacketWriter();
            writer.Write((ushort) PacketType.MsgGuideInfo);
            writer.Write((int) Mode);
            writer.Write(SenderIdentity);
            writer.Write(Identity);
            writer.Write(Mesh);
            writer.Write(SharedBattlePower);
            writer.Write(Unknown24);
            writer.Write(EnroleDate);
            writer.Write(Level);
            writer.Write(Profession);
            writer.Write(PkPoints);
            writer.Write(Syndicate);
            writer.Write(Unknown38);
            writer.Write(SyndicatePosition);
            writer.Write(Unknown40);
            writer.Write(0);
            writer.Write(0);
            writer.Write(IsOnline);
            writer.Write(Fill41);
            writer.Write(Unknown52);
            writer.Write(Experience);
            writer.Write(Blessing);
            writer.Write(Composition);
            writer.Write(Names);
            return writer.ToArray();
        }

        public enum RequestMode
        {
            None,
            Mentor,
            Apprentice
        }
    }
}