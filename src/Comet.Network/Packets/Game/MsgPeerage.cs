using System.Collections.Generic;

namespace Comet.Network.Packets.Game
{
    public abstract class MsgPeerage<T> : MsgBase<T>
    {
        public List<NobilityStruct> Rank = new();

        public List<string> Strings = new();

        public MsgPeerage()
        {
            Data = 0;
        }

        public MsgPeerage(NobilityAction action, ushort maxPerPage, ushort maxPages)
        {
            Action = action;
            DataLow2 = maxPages;
            DataHigh = maxPerPage;
        }

        public NobilityAction Action { get; set; }
        public ulong Data { get; set; }

        public uint DataLow
        {
            get => (uint) (Data - ((ulong) DataHigh << 32));
            set => Data = ((ulong) DataHigh << 32) | value;
        }

        public ushort DataLow1
        {
            get => (ushort) (DataLow - (DataLow2 << 16));
            set => DataLow = (uint) ((DataLow2 << 16) | value);
        }

        public ushort DataLow2
        {
            get => (ushort) (DataLow >> 16);
            set => DataLow = (uint) (value << 16) | DataLow;
        }

        public uint DataHigh
        {
            get => (uint) (Data >> 32);
            set => Data = ((ulong) value << 32) | Data;
        }

        public ushort DataHigh1
        {
            get => (ushort) (DataHigh - (DataHigh2 << 16));
            set => DataHigh = (uint) ((DataHigh2 << 16) | value);
        }

        public ushort DataHigh2
        {
            get => (ushort) (DataHigh >> 16);
            set => DataHigh = (uint) (value << 16) | DataHigh;
        }

        public uint Data1 { get; set; }
        public uint Data2 { get; set; }
        public uint Data3 { get; set; }
        public uint Data4 { get; set; }

        /// <summary>
        ///     Decodes a byte packet into the packet structure defined by this message class.
        ///     Should be invoked to structure data from the client for processing. Decoding
        ///     follows TQ Digital's byte ordering rules for an all-binary protocol.
        /// </summary>
        /// <param name="bytes">Bytes from the packet processor or client socket</param>
        public override void Decode(byte[] bytes)
        {
            var reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType) reader.ReadUInt16();
            Action = (NobilityAction) reader.ReadUInt32();
            Data = reader.ReadUInt64();
            Data1 = reader.ReadUInt32();
            Data2 = reader.ReadUInt32();
            Data3 = reader.ReadUInt32();
            Data4 = reader.ReadUInt32();
            Strings = reader.ReadStrings();
        }

        /// <summary>
        ///     Encodes the packet structure defined by this message class into a byte packet
        ///     that can be sent to the client. Invoked automatically by the client's send
        ///     method. Encodes using byte ordering rules interoperable with the game client.
        /// </summary>
        /// <returns>Returns a byte packet of the encoded packet.</returns>
        public override byte[] Encode()
        {
            var writer = new PacketWriter();
            writer.Write((ushort) PacketType.MsgPeerage);
            writer.Write((uint) Action); // 4
            writer.Write(Data);          // 8
            writer.Write(Data1);         // 16
            writer.Write(Data2);         // 20
            writer.Write(Data3);         // 24
            writer.Write(Data4);         // 28

            if (Action == NobilityAction.List)
                foreach (NobilityStruct rank in Rank)
                {
                    writer.Write(rank.Identity);
                    writer.Write(rank.LookFace);
                    writer.Write(rank.LookFace);
                    writer.Write(rank.Name, 16);
                    writer.Write(0);
                    writer.Write(rank.Donation);
                    writer.Write((uint) rank.Rank);
                    writer.Write(rank.Position);
                }
            else
                writer.Write(Strings);

            return writer.ToArray();
        }
    }
    public struct NobilityStruct
    {
        public uint Identity;
        public uint LookFace;
        public string Name;
        public ulong Donation;
        public NobilityRank Rank;
        public int Position;
    }

    public enum NobilityAction : uint
    {
        None,
        Donate,
        List,
        Info,
        QueryRemainingSilver
    }

    public enum NobilityRank : byte
    {
        Serf,
        Knight,
        Baron = 3,
        Earl = 5,
        Duke = 7,
        Prince = 9,
        King = 12
    }
}