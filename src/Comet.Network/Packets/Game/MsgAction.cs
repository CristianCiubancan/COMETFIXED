using System;
using System.Collections.Generic;
using Comet.Network.Packets;

namespace Comet.Game.Packets
{
    /// <remarks>Packet Type 1010</remarks>
    /// <summary>
    ///     Message containing a general action being performed by the client. Commonly used
    ///     as a request-response protocol for question and answer like exchanges. For example,
    ///     walk requests are responded to with an answer as to if the step is legal or not.
    /// </summary>
    public abstract class MsgAction<T> : MsgBase<T>
    {
        protected MsgAction()
        {
            Timestamp = (uint) Environment.TickCount;
        }

        // Packet Properties
        public uint Timestamp { get; set; }
        public uint Identity { get; set; }
        public uint Data { get; set; }
        public uint Command { get; set; }

        public ushort CommandX
        {
            get => (ushort) (Command - (CommandY << 16));
            set => Command = (uint) ((CommandY << 16) | value);
        }

        public ushort CommandY
        {
            get => (ushort) (Command >> 16);
            set => Command = (uint) (value << 16) | Command;
        }

        public uint Argument { get; set; }

        public ushort ArgumentX
        {
            get => (ushort) (Argument - (ArgumentY << 16));
            set => Argument = (uint) ((ArgumentY << 16) | value);
        }

        public ushort ArgumentY
        {
            get => (ushort) (Argument >> 16);
            set => Argument = (uint) (value << 16) | Argument;
        }

        public ushort Direction { get; set; }
        public ActionType Action { get; set; }
        public ushort X { get; set; }
        public ushort Y { get; set; }
        public uint Map { get; set; }
        public uint MapColor { get; set; }
        public List<string> Strings { get; } = new();

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
            Identity = reader.ReadUInt32();            // 4
            Command = reader.ReadUInt32();             // 8
            Argument = reader.ReadUInt32();            // 12
            Timestamp = reader.ReadUInt32();           // 16
            Action = (ActionType) reader.ReadUInt16(); // 20
            Direction = reader.ReadUInt16();           // 22 
            X = reader.ReadUInt16();                   // 24 
            Y = reader.ReadUInt16();                   // 26
            Map = reader.ReadUInt32();                 // 28
            MapColor = reader.ReadUInt32();            // 32
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
            writer.Write((ushort) PacketType.MsgAction);
            writer.Write(Identity);
            writer.Write(Command);
            writer.Write(Argument);
            writer.Write(Timestamp);
            writer.Write((ushort) Action);
            writer.Write(Direction);
            writer.Write(X);
            writer.Write(Y);
            writer.Write(Map);
            writer.Write(MapColor);
            //writer.Write((byte)0);
            //writer.Write(Strings);
            return writer.ToArray();
        }

        /// <summary>
        ///     Defines actions that may be requested by the user, or given to by the server.
        ///     Allows for action handling as a packet subtype. Enums should be named by the
        ///     action they provide to a system in the context of the player actor.
        /// </summary>
        public enum ActionType
        {
            LoginSpawn = 74,
            LoginInventory,
            LoginRelationships,
            LoginProficiencies,
            LoginSpells,
            CharacterDirection,
            CharacterEmote = 81,
            MapPortal = 85,
            MapTeleport,
            CharacterLevelUp = 92,
            SpellAbortXp,
            CharacterRevive,
            CharacterDelete,
            CharacterPkMode,
            LoginGuild,
            MapMine = 99,
            MapTeamLeaderStar = 101,
            MapQuery,
            AbortMagic = 103,
            MapArgb = 104,
            MapTeamMemberStar = 106,
            Kickback = 108,
            SpellRemove,
            ProficiencyRemove,
            BoothSpawn,
            BoothSuspend,
            BoothResume,
            BoothLeave,
            ClientCommand = 116,
            CharacterObservation,
            SpellAbortTransform,
            SpellAbortFlight = 120,
            MapGold,
            RelationshipsEnemy = 123,
            ClientDialog = 126,
            CallPetJump = 130,
            LoginComplete = 132,
            MapEffect = 134,
            RemoveEntity = 135,
            MapJump = 137,
            CharacterDead = 145,
            RelationshipsFriend = 148,
            CharacterAvatar = 151,
            QueryTradeBuddy = 143,
            ItemDetained = 153,
            ItemDetainedEx = 155,
            NinjaStep = 156,
            Away = 161,
            PathFinding = 162,
            ProgressBar = 164,

            //SetGhost = 145,
            FriendObservation = 310,

            MapFarJump = 10000, // monster only
            MapJumpBlock        // monster only
        }
    }
}