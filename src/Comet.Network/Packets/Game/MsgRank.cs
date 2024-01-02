using System.Collections.Generic;

namespace Comet.Network.Packets.Game
{
    public abstract class MsgRank<T> : MsgBase<T>
    {
        public RequestType Mode { get; set; }
        public uint Identity { get; set; }
        public RankType RankMode { get; set; }
        public byte Subtype { get; set; }
        public ushort PageNumber { get; set; }
        public ushort Data1 { get; set; }
        public ushort Data2 { get; set; }
        public ushort Data3 { get; set; }

        public List<string> Strings { get; set; } = new();
        public List<QueryStruct> Infos { get; set; } = new();

        public override void Decode(byte[] bytes)
        {
            var reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();             // 0
            Type = (PacketType) reader.ReadUInt16();  // 2
            Mode = (RequestType) reader.ReadUInt32(); // 4
            Identity = reader.ReadUInt32();           // 8
            RankMode = (RankType) reader.ReadByte();  // 12
            Subtype = reader.ReadByte();              // 13
            PageNumber = reader.ReadUInt16();         // 18
        }

        public override byte[] Encode()
        {
            var writer = new PacketWriter();
            writer.Write((ushort) PacketType.MsgRank);
            writer.Write((uint) Mode);     // 4
            writer.Write(Identity);        // 8
            writer.Write((byte) RankMode); // 12
            writer.Write(Subtype);         // 13
            writer.Write(PageNumber);      // 14
            writer.Write(Infos.Count);     // 16
            writer.Write(0);               // 20
            foreach (QueryStruct info in Infos)
            {
                writer.Write(info.Type);     // 24
                writer.Write(info.Amount);   // 32
                writer.Write(info.Identity); // 40
                writer.Write(info.Identity); // 44
                writer.Write(info.Name, 16); // 48
                writer.Write(info.Name, 16); // 64
            }

            return writer.ToArray();
        }

        public struct QueryStruct
        {
            public ulong Type;
            public ulong Amount;
            public uint Identity;
            public string Name;
        }

        public enum RankType : byte
        {
            Flower,
            ChiDragon,
            ChiPhoenix,
            ChiTiger,
            ChiTurtle
        }

        public enum RequestType
        {
            None,
            RequestRank,
            QueryInfo,
            QueryIcon = 5
        }
    }
}