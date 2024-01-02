using System.Collections.Generic;

namespace Comet.Network.Packets.Game
{
    public abstract class MsgNpcInfoEx<T> : MsgBase<T>
    {
        public uint Identity { get; set; }
        public uint MaxLife { get; set; }
        public uint Life { get; set; }
        public ushort PosX { get; set; }
        public ushort PosY { get; set; }
        public ushort Lookface { get; set; }
        public ushort NpcType { get; set; }
        public ushort Sort { get; set; }
        public string Name { get; set; }

        /// <summary>
        ///     Encodes the packet structure defined by this message class into a byte packet
        ///     that can be sent to the client. Invoked automatically by the client's send
        ///     method. Encodes using byte ordering rules interoperable with the game client.
        /// </summary>
        /// <returns>Returns a byte packet of the encoded packet.</returns>
        public override byte[] Encode()
        {
            var writer = new PacketWriter();
            writer.Write((ushort) PacketType.MsgNpcInfoEx); // 2
            writer.Write(Identity);                         // 4
            writer.Write(MaxLife);                          // 8
            writer.Write(Life);                             // 12
            writer.Write(PosX);                             // 16
            writer.Write(PosY);                             // 18
            writer.Write(Lookface);                         // 20
            writer.Write(NpcType);                          // 22
            writer.Write(Sort);                             // 24
            if (!string.IsNullOrEmpty(Name))
                writer.Write(new List<string> {Name});
            else writer.Write(0);
            return writer.ToArray();
        }
    }
}