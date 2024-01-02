namespace Comet.Network.Packets.Game
{
    public abstract class MsgWeaponSkill<T> : MsgBase<T>
    {
        public uint Identity { get; set; }
        public uint Level { get; set; }
        public uint Experience { get; set; }

        /// <summary>
        ///     Encodes the packet structure defined by this message class into a byte packet
        ///     that can be sent to the client. Invoked automatically by the client's send
        ///     method. Encodes using byte ordering rules interoperable with the game client.
        /// </summary>
        /// <returns>Returns a byte packet of the encoded packet.</returns>
        public override byte[] Encode()
        {
            var writer = new PacketWriter();
            writer.Write((ushort) PacketType.MsgWeaponSkill);
            writer.Write(Identity);
            writer.Write(Level);
            writer.Write(Experience);
            return writer.ToArray();
        }

        public static readonly uint[] RequiredExperience = new uint[21]
        {
            0,
            1200,
            68000,
            250000,
            640000,
            1600000,
            4000000,
            10000000,
            22000000,
            40000000,
            90000000,
            95000000,
            142500000,
            213750000,
            320625000,
            480937500,
            721406250,
            1082109375,
            1623164063,
            2100000000,
            0
        };
    }
}