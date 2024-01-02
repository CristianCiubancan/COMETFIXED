namespace Comet.Network.Packets.Game
{
    public abstract class MsgWalk<T> : MsgBase<T>
    {
        public uint Identity { get; set; }
        public byte Direction { get; set; }
        public byte Mode { get; set; }
        public ushort Padding { get; set; }

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
            Direction = (byte) reader.ReadUInt32();
            Identity = reader.ReadUInt32();
            Mode = reader.ReadByte();
            Padding = reader.ReadUInt16();
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
            writer.Write((ushort) PacketType.MsgWalk);
            writer.Write((int) Direction);
            writer.Write(Identity);
            writer.Write(Mode);
            writer.Write(Padding);
            return writer.ToArray();
        }
    }

    public enum RoleMoveMode
    {
        Walk = 0,

        // PathMove()
        Run,
        Shift,

        // to server only
        Jump,
        Trans,
        Chgmap,
        JumpMagicAttack,
        Collide,
        Synchro,

        // to server only
        Track,

        RunDir0 = 20,

        RunDir7 = 27
    }
}