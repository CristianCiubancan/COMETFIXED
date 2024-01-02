namespace Comet.Network.Packets.Game
{
    public abstract class MsgFriendInfo<T> : MsgBase<T>
    {
        public uint Identity { get; set; }
        public uint Lookface { get; set; }
        public byte Level { get; set; }
        public byte Profession { get; set; }
        public ushort PkPoints { get; set; }
        public ushort SyndicateIdentity { get; set; }
        public ushort SyndicateRank { get; set; }
        public string Mate { get; set; }
        public bool IsEnemy { get; set; }

        /// <summary>
        ///     Encodes the packet structure defined by this message class into a byte packet
        ///     that can be sent to the client. Invoked automatically by the client's send
        ///     method. Encodes using byte ordering rules interoperable with the game client.
        /// </summary>
        /// <returns>Returns a byte packet of the encoded packet.</returns>
        public override byte[] Encode()
        {
            var writer = new PacketWriter();
            writer.Write((ushort) PacketType.MsgFriendInfo);
            writer.Write(Identity);
            writer.Write(Lookface);
            writer.Write(Level);
            writer.Write(Profession);
            writer.Write(PkPoints);
            writer.Write(SyndicateIdentity);
            writer.Write(SyndicateRank);
            writer.Write(Mate, 16);
            writer.Write(IsEnemy);
            return writer.ToArray();
        }
    }
}