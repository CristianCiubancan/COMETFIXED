using System.IO;

namespace Comet.Network.Packets.Game
{
    public abstract class MsgItemInfo<T> : MsgBase<T>
    {
        public uint Identity { get; set; }
        public uint Itemtype { get; set; }
        public ushort Amount { get; set; }
        public ushort AmountLimit { get; set; }
        public ItemMode Mode { get; set; }
        public ushort Position { get; set; }
        public uint SocketProgress { get; set; }
        public byte SocketOne { get; set; }
        public byte SocketTwo { get; set; }
        public byte Effect { get; set; }
        public byte Plus { get; set; }
        public byte Bless { get; set; }
        public byte Enchantment { get; set; }
        public int AntiMonster { get; set; }
        public bool IsSuspicious { get; set; }
        public byte Color { get; set; }
        public bool IsLocked { get; set; }
        public bool IsBound { get; set; }
        public uint CompositionProgress { get; set; }
        public bool Inscribed { get; set; }

        /// <summary>
        ///     Encodes the packet structure defined by this message class into a byte packet
        ///     that can be sent to the client. Invoked automatically by the client's send
        ///     method. Encodes using byte ordering rules interoperable with the game client.
        /// </summary>
        /// <returns>Returns a byte packet of the encoded packet.</returns>
        public override byte[] Encode()
        {
            var writer = new PacketWriter();
            writer.Write((ushort) PacketType.MsgItemInfo);
            writer.Write(Identity);            // 4
            writer.Write(Itemtype);            // 8
            writer.Write(Amount);              // 12
            writer.Write(AmountLimit);         // 14
            writer.Write((ushort) Mode);       // 16
            writer.Write(Position);            // 18
            writer.Write(SocketProgress);      // 20
            writer.Write(SocketOne);           // 24
            writer.Write(SocketTwo);           // 25
            writer.Write(Effect);              // 26
            writer.Write((byte) 0);            // 27
            writer.Write(Plus);                // 28
            writer.Write(Bless);               // 29
            writer.Write(IsBound);             // 30
            writer.Write(Enchantment);         // 31
            writer.Write(AntiMonster);         // 32
            writer.Write(IsSuspicious);        // 36
            writer.Write((byte) 0);            // 37
            writer.Write(IsLocked);            // 38
            writer.Write((byte) 0);            // 39
            writer.Write((int) Color);         // 40
            writer.Write(CompositionProgress); // 44
            writer.BaseStream.Seek(2, SeekOrigin.Current);
            writer.Write(Inscribed ? 1 : 0);
            return writer.ToArray();
        }

        public enum ItemMode : ushort
        {
            Default = 0x01,
            Trade = 0x02,
            Update = 0x03,
            View = 0x04
        }
    }
}