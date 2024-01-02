using System;
using System.Collections.Generic;

namespace Comet.Network.Packets.Updater
{
    public abstract class MsgUpdLogin<T> : MsgBase<T>
    {
        public int Timestamp { get; set; }
        public string CurrentFileHash { get; set; }
        public string MacAddress { get; set; }
        public string UserName { get; set; }
        public string MachineName { get; set; }
        public string MachineDomain { get; set; }
        public string WindowsVersion { get; set; }
        public List<string> IpAddresses { get; set; } = new();

        /// <inheritdoc />
        public override byte[] Encode()
        {
            PacketWriter writer = new PacketWriter();
            writer.Write((ushort) PacketType.MsgUpdLogin);
            writer.Write(Timestamp = Environment.TickCount);
            writer.Write(CurrentFileHash);
            writer.Write(MacAddress);
            writer.Write(UserName);
            writer.Write(MachineName);
            writer.Write(MachineDomain);
            writer.Write(WindowsVersion);
            writer.Write(IpAddresses);
            return writer.ToArray();
        }

        /// <inheritdoc />
        public override void Decode(byte[] bytes)
        {
            PacketReader reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType) reader.ReadUInt16();
            Timestamp = reader.ReadInt32();
            CurrentFileHash = reader.ReadString();
            MacAddress = reader.ReadString();
            UserName = reader.ReadString();
            MachineName = reader.ReadString();
            MachineDomain = reader.ReadString();
            WindowsVersion = reader.ReadString();
            IpAddresses = reader.ReadStrings();
        }
    }
}
