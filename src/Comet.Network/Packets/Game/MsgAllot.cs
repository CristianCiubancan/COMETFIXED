﻿namespace Comet.Network.Packets.Game
{
    public abstract class MsgAllot<T> : MsgBase<T>
    {
        public ushort Force { get; set; }
        public ushort Speed { get; set; }
        public ushort Health { get; set; }
        public ushort Soul { get; set; }

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
            Force = reader.ReadByte();
            Speed = reader.ReadByte();
            Health = reader.ReadByte();
            Soul = reader.ReadByte();
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
            writer.Write((ushort) PacketType.MsgAllot);
            writer.Write((byte) Force);
            writer.Write((byte) Speed);
            writer.Write((byte) Health);
            writer.Write((byte) Soul);
            return writer.ToArray();
        }
    }
}