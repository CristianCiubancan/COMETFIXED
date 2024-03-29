﻿namespace Comet.Network.Packets.Game
{
    public abstract class MsgSynpOffer<T> : MsgBase<T>
    {
        public uint Identity { get; set; }
        public int Silver { get; set; }
        public uint ConquerPoints { get; set; }
        public uint GuideDonation { get; set; }
        public int PkDonation { get; set; }
        public uint ArsenalDonation { get; set; }
        public uint RedRoseDonation { get; set; }
        public uint WhiteRoseDonation { get; set; }
        public uint OrchidDonation { get; set; }
        public uint TulipDonation { get; set; }
        public uint SilverTotal { get; set; }
        public uint ConquerPointsTotal { get; set; }
        public uint GuideTotal { get; set; }
        public int PkTotal { get; set; }

        public override void Decode(byte[] bytes)
        {
            var reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType) reader.ReadUInt16();
            Identity = reader.ReadUInt32();
            Silver = reader.ReadInt32();
            ConquerPoints = reader.ReadUInt32();
            GuideDonation = reader.ReadUInt32();
            ArsenalDonation = reader.ReadUInt32();
            RedRoseDonation = reader.ReadUInt32();
            WhiteRoseDonation = reader.ReadUInt32();
            OrchidDonation = reader.ReadUInt32();
            TulipDonation = reader.ReadUInt32();
        }

        public override byte[] Encode()
        {
            var writer = new PacketWriter();
            writer.Write((ushort) PacketType.MsgSynpOffer);
            writer.Write(Identity);           // 4
            writer.Write(Silver);             // 8
            writer.Write(ConquerPoints);      // 12
            writer.Write(GuideDonation);      // 16
            writer.Write(PkDonation);         // 20
            writer.Write(ArsenalDonation);    // 24
            writer.Write(RedRoseDonation);    // 28
            writer.Write(WhiteRoseDonation);  // 32
            writer.Write(OrchidDonation);     // 36
            writer.Write(TulipDonation);      // 40
            writer.Write(SilverTotal);        // 44 Total Silver
            writer.Write(ConquerPointsTotal); // 48 Total Emoney
            writer.Write(GuideTotal);         // 52 Guide
            writer.Write(PkTotal);            // 56 PK
            return writer.ToArray();
        }
    }
}