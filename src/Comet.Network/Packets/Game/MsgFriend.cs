namespace Comet.Network.Packets.Game
{
    public abstract class MsgFriend<T> : MsgBase<T>
    {
        public uint Identity { get; set; }
        public MsgFriendAction Action { get; set; }
        public bool Online { get; set; }
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
            Length = reader.ReadUInt16();                 // 0
            Type = (PacketType) reader.ReadUInt16();      // 2
            Identity = reader.ReadUInt32();               // 4
            Action = (MsgFriendAction) reader.ReadByte(); // 8
            Online = reader.ReadBoolean();                // 9
            reader.ReadInt16();                           // 10
            reader.ReadInt32();                           // 12
            reader.ReadInt32();                           // 16
            Name = reader.ReadString(16);                 // 20
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
            writer.Write((ushort) PacketType.MsgFriend);
            writer.Write(Identity);
            writer.Write((byte) Action);
            writer.Write(Online);
            writer.Write((ushort) 0);
            writer.Write(0);
            writer.Write(0);
            writer.Write(Name, 16);
            return writer.ToArray();
        }

        public enum MsgFriendAction : byte
        {
            RequestFriend = 10,
            NewFriend = 11,
            SetOnlineFriend = 12,
            SetOfflineFriend = 13,
            RemoveFriend = 14,
            AddFriend = 15,
            SetOnlineEnemy = 16,
            SetOfflineEnemy = 17,
            RemoveEnemy = 18,
            AddEnemy = 19
        }
    }
}