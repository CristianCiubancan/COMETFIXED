using System.Collections.Generic;

namespace Comet.Network.Packets.Game
{
    public abstract class MsgName<T> : MsgBase<T>
    {
        public List<string> Strings = new();
        public int Timestamp { get; set; }
        public uint Identity { get; set; }

        public ushort X
        {
            get => (ushort) (Identity - (Y << 16));
            set => Identity = (uint) ((Y << 16) | value);
        }

        public ushort Y
        {
            get => (ushort) (Identity >> 16);
            set => Identity = (uint) (value << 16) | Identity;
        }

        public StringAction Action { get; set; }

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
            Action = (StringAction) reader.ReadByte();
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
            writer.Write((ushort) PacketType.MsgName);
            //writer.Write(Timestamp); // 4
            writer.Write(Identity);      // 8
            writer.Write((byte) Action); // 12
            writer.Write(Strings);       // 13
            return writer.ToArray();
        }
    }

    public enum StringAction : byte
    {
        None = 0,
        Fireworks,
        CreateGuild,
        Guild,
        ChangeTitle,
        DeleteRole = 5,
        Mate,
        QueryNpc,
        Wanted,
        MapEffect,
        RoleEffect = 10,
        MemberList,
        KickoutGuildMember,
        QueryWanted,
        QueryPoliceWanted,
        PoliceWanted = 15,
        QueryMate,
        AddDicePlayer,
        DeleteDicePlayer,
        DiceBonus,
        PlayerWave = 20,
        SetAlly,
        SetEnemy,
        WhisperWindowInfo = 26
    }
}