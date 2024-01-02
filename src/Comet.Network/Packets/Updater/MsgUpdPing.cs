using System;

namespace Comet.Network.Packets.Updater
{
    public abstract class MsgUpdPing<T> : MsgBase<T>
    {
        protected MsgUpdPing()
        {
            Timestamp = Environment.TickCount;
        }

        public int Timestamp { get; private set; }

        /// <inheritdoc />
        public override void Decode(byte[] bytes)
        {
            var reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType) reader.ReadUInt16();
            Timestamp = reader.ReadInt32();
        }

        /// <inheritdoc />
        public override byte[] Encode()
        {
            var writer = new PacketWriter();
            writer.Write((ushort) PacketType.MsgUpdPing);
            writer.Write(Timestamp);
            return writer.ToArray();
        }
    }
}