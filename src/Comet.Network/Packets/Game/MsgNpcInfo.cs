using System.Collections.Generic;

namespace Comet.Network.Packets.Game
{
    public abstract class MsgNpcInfo<T> : MsgBase<T>
    {
        public uint Identity { get; set; }
        public ushort PosX { get; set; }
        public ushort PosY { get; set; }
        public ushort Lookface { get; set; }
        public ushort NpcType { get; set; }
        public ushort Sort { get; set; }
        public byte Unknown0 { get; set; }
        public byte Unknown1 { get; set; }
        public string Name { get; set; }

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
            Identity = reader.ReadUInt32();
            PosX = reader.ReadUInt16();
            PosY = reader.ReadUInt16();
            Lookface = reader.ReadUInt16();
            NpcType = reader.ReadUInt16();
            Unknown0 = reader.ReadByte();
            Unknown1 = reader.ReadByte();
            List<string> names = reader.ReadStrings();
            if (names.Count > 0)
                Name = names[0];
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
            writer.Write((ushort) PacketType.MsgNpcInfo);
            writer.Write(Identity);
            writer.Write(PosX);
            writer.Write(PosY);
            writer.Write(Lookface);
            writer.Write(NpcType);
            writer.Write(Sort);
            writer.Write(Unknown0);
            writer.Write(Unknown1);
            writer.Write(new List<string> {Name});
            return writer.ToArray();
        }
    }
}