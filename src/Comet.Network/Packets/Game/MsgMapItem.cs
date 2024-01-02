namespace Comet.Network.Packets.Game
{
    public abstract class MsgMapItem<T> : MsgBase<T>
    {
        public ushort Color;
        public uint Identity;
        public uint Itemtype;
        public ushort MapX;
        public ushort MapY;
        public DropType Mode;
        public int Timestamp;

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
            //Timestamp = reader.ReadInt32();
            Identity = reader.ReadUInt32();
            Itemtype = reader.ReadUInt32();
            MapX = reader.ReadUInt16();
            MapY = reader.ReadUInt16();
            Color = reader.ReadUInt16();
            Mode = (DropType) reader.ReadUInt16();
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
            writer.Write((ushort) PacketType.MsgMapItem);
            //writer.Write(Timestamp); // 4
            writer.Write(Identity);      // 8
            writer.Write(Itemtype);      // 12
            writer.Write(MapX);          // 16
            writer.Write(MapY);          // 18
            writer.Write(Color);         // 20
            writer.Write((ushort) Mode); // 22
            writer.Write((byte) 0);      // 24
            writer.Write((byte) 0);      // 25
            writer.Write((byte) 0);      // 26
            writer.Write((byte) 0);      // 27
            return writer.ToArray();
        }

        public enum DropType : ushort
        {
            Unknown = 0,
            LayItem = 1,
            DisappearItem = 2,
            PickupItem = 3,
            DetainItem = 4,
            LayTrap = 10,
            SynchroTrap = 11,
            DropTrap = 12
        }
    }
}