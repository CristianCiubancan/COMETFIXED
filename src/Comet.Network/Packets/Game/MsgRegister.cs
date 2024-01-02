using System.IO;

namespace Comet.Network.Packets.Game
{
    /// <remarks>Packet Type 1001</remarks>
    /// <summary>
    ///     Message containing character creation details, such as the new character's name,
    ///     body size, and profession. The character name should be verified, and may be
    ///     rejected by the server if a character by that name already exists.
    /// </summary>
    public abstract class MsgRegister<T> : MsgBase<T>
    {
        // Packet Properties
        public string Username { get; set; }
        public string CharacterName { get; set; }
        public ushort Mesh { get; set; }
        public ushort Class { get; set; }
        public uint Token { get; set; }

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
            Username = reader.ReadString(16);
            CharacterName = reader.ReadString(16);
            reader.BaseStream.Seek(16, SeekOrigin.Current);
            Mesh = reader.ReadUInt16();
            Class = reader.ReadUInt16();
            Token = reader.ReadUInt32();
        }
    }
}