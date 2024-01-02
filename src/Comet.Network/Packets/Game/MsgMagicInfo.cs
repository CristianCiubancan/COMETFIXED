namespace Comet.Network.Packets.Game
{
    public abstract class MsgMagicInfo<T> : MsgBase<T>
    {
        public uint Experience { get; set; }
        public ushort Magictype { get; set; }
        public ushort Level { get; set; }

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
            Experience = reader.ReadUInt32();
            Magictype = reader.ReadUInt16();
            Level = reader.ReadUInt16();
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
            writer.Write((ushort) PacketType.MsgMagicInfo);
            writer.Write(Experience);
            writer.Write(Magictype);
            writer.Write(Level);
            return writer.ToArray();
        }
    }
}