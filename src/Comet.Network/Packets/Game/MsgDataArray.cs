using System.Collections.Generic;

namespace Comet.Network.Packets.Game
{
    public abstract class MsgDataArray<T> : MsgBase<T>
    {
        public List<uint> Items = new();

        public DataArrayMode Action { get; set; }
        public byte Count { get; set; }

        public override void Decode(byte[] bytes)
        {
            PacketReader reader = new(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType) reader.ReadUInt16();
            Action = (DataArrayMode) reader.ReadByte();
            Count = reader.ReadByte();
            reader.ReadUInt16();
            for (var i = 0; i < Count; i++) Items.Add(reader.ReadUInt32());
        }

        public override byte[] Encode()
        {
            PacketWriter writer = new();
            writer.Write((ushort) Type);
            writer.Write((byte) Action);
            writer.Write((byte) Items.Count);
            writer.Write((ushort) 0);
            foreach (uint item in Items) writer.Write(item);
            return writer.ToArray();
        }

        public enum DataArrayMode : byte
        {
            Composition = 0,
            CompositionSteedOriginal = 2,
            CompositionSteedNew = 3,
            QuickCompose = 4,
            QuickComposeMount = 5,
            UpgradeItemLevel = 6,
            UpgradeItemQuality = 7
        }
    }
}