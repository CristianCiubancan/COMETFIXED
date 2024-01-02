using System.Collections.Generic;

namespace Comet.Network.Packets.Game
{
    public abstract class MsgTitle<T> : MsgBase<T>
    {
        public TitleAction Action;
        public byte Count;
        public uint Identity;
        public byte Title;
        public List<byte> Titles = new();

        public override void Decode(byte[] bytes)
        {
            var reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType) reader.ReadUInt16();
            Identity = reader.ReadUInt32();
            Title = reader.ReadByte();
            Action = (TitleAction) reader.ReadByte();
            Count = reader.ReadByte();
            for (var i = 0; i < Count; i++)
                Titles.Add(reader.ReadByte());
        }

        public override byte[] Encode()
        {
            var writer = new PacketWriter();
            writer.Write((ushort) PacketType.MsgTitle);
            writer.Write(Identity); // 4
            writer.Write(Title);
            writer.Write((byte) Action);
            writer.Write(Count = (byte) Titles.Count);
            foreach (byte b in Titles)
                writer.Write(b);
            return writer.ToArray();
        }

        public enum TitleAction : byte
        {
            Hide = 0,
            Add = 1,
            Remove = 2,
            Select = 3,
            Query = 4
        }
    }
}