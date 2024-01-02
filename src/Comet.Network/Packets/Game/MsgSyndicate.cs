using System.Collections.Generic;

namespace Comet.Network.Packets.Game
{
    public abstract class MsgSyndicate<T> : MsgBase<T>
    {
        public SyndicateRequest Mode { get; set; }
        public uint Identity { get; set; }
        public int ConditionLevel { get; set; }
        public int ConditionMetempsychosis { get; set; }
        public int ConditionProfession { get; set; }
        public List<string> Strings { get; set; } = new();

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
            Mode = (SyndicateRequest) reader.ReadUInt32();
            Identity = reader.ReadUInt32();
            ConditionLevel = reader.ReadInt32();
            ConditionMetempsychosis = reader.ReadInt32();
            ConditionProfession = reader.ReadInt32();
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
            writer.Write((ushort) PacketType.MsgSyndicate);
            writer.Write((uint) Mode);
            writer.Write(Identity);
            writer.Write(ConditionLevel);
            writer.Write(ConditionMetempsychosis);
            writer.Write(ConditionProfession);
            writer.Write(Strings);
            return writer.ToArray();
        }

        public enum SyndicateRequest : uint
        {
            JoinRequest = 1,
            InviteRequest = 2,
            Quit = 3,
            Query = 6,
            Ally = 7,
            Unally = 8,
            Enemy = 9,
            Unenemy = 10,
            DonateSilvers = 11,
            Refresh = 12,
            Disband = 19,
            DonateConquerPoints = 20,
            SetRequirements = 24,
            Bulletin = 27,
            Promotion = 28,
            AcceptRequest = 29,
            Discharge = 30, // normal position
            Resign = 32,
            Discharge2 = 33,
            PaidPromotion = 34,
            DischargePaid = 36, // paid position
            PromotionList = 37
        }
    }
}