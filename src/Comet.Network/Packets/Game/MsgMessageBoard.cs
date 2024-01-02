using System.Collections.Generic;

namespace Comet.Network.Packets.Game
{
    public abstract class MsgMessageBoard<T> : MsgBase<T>
    {
        public List<string> Messages = new();

        public ushort Index { get; set; }
        public BoardChannel Channel { get; set; }
        public BoardAction Action { get; set; }

        public override void Decode(byte[] bytes)
        {
            var reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType) reader.ReadUInt16();
            Index = reader.ReadUInt16();
            Channel = (BoardChannel) reader.ReadUInt16();
            Action = (BoardAction) reader.ReadByte();
            Messages = reader.ReadStrings();
        }

        public override byte[] Encode()
        {
            var writer = new PacketWriter();
            writer.Write((ushort) PacketType.MsgMessageBoard);
            writer.Write(Index);
            writer.Write((ushort) Channel);
            writer.Write((byte) Action);
            writer.Write(Messages);
            return writer.ToArray();
        }

        public enum BoardAction : byte
        {
            None = 0,
            Del = 1,     // to server					// no return
            GetList = 2, // to server: index(first index)
            List = 3,    // to client: index(first index), name, words, time...
            GetWords = 4 // to server: index(for get)	// return by MsgTalk
        }

        public enum BoardChannel : ushort
        {
            None = 0,
            MsgTrade = 2201,
            MsgFriend = 2202,
            MsgTeam = 2203,
            MsgSyn = 2204,
            MsgOther = 2205,
            MsgSystem = 2206
        }
    }
}