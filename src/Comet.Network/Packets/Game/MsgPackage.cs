using System.Collections.Generic;

namespace Comet.Network.Packets.Game
{
    public abstract class MsgPackage<T> : MsgBase<T>
    {
        public List<WarehouseItem> Items = new();
        public uint Identity { get; set; }
        public WarehouseMode Action { get; set; }
        public StorageType Mode { get; set; }
        public ushort Unknown { get; set; }
        public uint Param { get; set; }

        public override void Decode(byte[] bytes)
        {
            var reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType) reader.ReadUInt16();
            Identity = reader.ReadUInt32();
            Action = (WarehouseMode) reader.ReadByte();
            Mode = (StorageType) reader.ReadByte();
            Unknown = reader.ReadUInt16();
            Param = reader.ReadUInt32();
        }

        public override byte[] Encode()
        {
            var writer = new PacketWriter();
            writer.Write((ushort) PacketType.MsgPackage);
            writer.Write(Identity);
            writer.Write((byte) Action);
            writer.Write((byte) Mode);
            writer.Write((ushort) 0);
            if (Items.Count > 0)
            {
                writer.Write(Items.Count);
                foreach (WarehouseItem item in Items)
                {
                    writer.Write(item.Identity);            // 0
                    writer.Write(item.Type);                // 4
                    writer.Write(item.Ident);               // 8
                    writer.Write(item.SocketOne);           // 9
                    writer.Write(item.SocketTwo);           // 10
                    writer.Write(item.Magic1);              // 11
                    writer.Write(item.Magic2);              // 12
                    writer.Write(item.Magic3);              // 13
                    writer.Write((byte) item.Blessing);     // 14
                    writer.Write(item.Bound);               // 15
                    writer.Write(item.Enchantment);         // 16
                    writer.Write(item.AntiMonster);         // 18
                    writer.Write(item.Suspicious);          // 20
                    writer.Write((byte) 0);                 // 21
                    writer.Write(item.Locked);              // 22
                    writer.Write(item.Color);               // 23
                    writer.Write(item.SocketProgress);      // 24
                    writer.Write(item.CompositionProgress); // 28
                    writer.Write(item.Inscribed);
                }
            }
            else
            {
                writer.Write(Param);
            }

            return writer.ToArray();
        }

        public struct WarehouseItem
        {
            public uint Identity;
            public uint Type;
            public byte Ident;
            public byte SocketOne;
            public byte SocketTwo;
            public byte Magic1;
            public byte Magic2;
            public byte Magic3;
            public ushort Blessing;
            public bool Bound;
            public ushort Enchantment;
            public ushort AntiMonster;
            public bool Suspicious;
            public bool Locked;
            public byte Color;
            public uint SocketProgress;
            public uint CompositionProgress;
            public int Inscribed;
        }

        public enum StorageType : byte
        {
            None = 0,
            Storage = 10,
            Trunk = 20,
            Chest = 30
        }

        public enum WarehouseMode : byte
        {
            Query = 0,
            CheckIn,
            CheckOut
        }
    }
}