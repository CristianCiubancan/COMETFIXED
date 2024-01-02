using System;

namespace Comet.Network.Packets.Updater
{
    public abstract class MsgUpdLoginEx<T> : MsgBase<T>
    {
        public int Timestamp { get; private set; }
        public UpdLoginEx Response { get; set; }

        /// <inheritdoc />
        public override byte[] Encode()
        {
            PacketWriter writer = new PacketWriter();
            writer.Write((ushort) PacketType.MsgUpdLoginEx);
            writer.Write(Timestamp = Environment.TickCount);
            writer.Write((int) Response);
            return writer.ToArray();
        }

        /// <inheritdoc />
        public override void Decode(byte[] bytes)
        {
            PacketReader reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType) reader.ReadUInt16();
            Timestamp = reader.ReadInt32();
            Response = (UpdLoginEx) reader.ReadInt32();
        }
    }

    public enum UpdLoginEx
    {
        Success,
        InvalidClientData,
        MacAddressAlreadySignedIn,
        ComputerAlreadySignedIn,
        IncompatibleWindowsVersion,
        InvalidMacAddress,
        InvalidIpAddress,
        GuidHacking,
        InvalidConquerMd5
    }
}
