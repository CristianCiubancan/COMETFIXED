namespace Comet.Network.Packets.Game
{
    public abstract class MsgTeam<T> : MsgBase<T>
    {
        public TeamAction Action { get; set; }
        public uint Identity { get; set; }

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
            Action = (TeamAction) reader.ReadUInt32();
            Identity = reader.ReadUInt32();
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
            writer.Write((ushort) PacketType.MsgTeam);
            writer.Write((uint) Action);
            writer.Write(Identity);
            return writer.ToArray();
        }

        public enum TeamAction
        {
            Create = 0x00,
            RequestJoin = 0x01,
            LeaveTeam = 0x02,
            AcceptInvite = 0x03,
            RequestInvite = 0x04,
            AcceptJoin = 0x05,
            Dismiss = 0x06,
            Kick = 0x07,
            Forbid = 0x08,
            RemoveForbid = 0x09,
            CloseMoney = 0x0A,
            OpenMoney = 0x0B,
            CloseItem = 0x0C,
            OpenItem = 0x0D
        }
    }
}