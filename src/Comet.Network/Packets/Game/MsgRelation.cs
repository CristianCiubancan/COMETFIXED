namespace Comet.Network.Packets.Game
{
    public abstract class MsgRelation<T> : MsgBase<T>
    {
        public int BattlePower;
        public bool IsSpouse;
        public bool IsTradePartner;
        public bool IsTutor;
        public int Level;
        public uint SenderIdentity;
        public uint TargetIdentity;

        public override byte[] Encode()
        {
            var writer = new PacketWriter();
            writer.Write((ushort) PacketType.MsgRelation);
            writer.Write(SenderIdentity);
            writer.Write(TargetIdentity);
            writer.Write(Level);
            writer.Write(BattlePower);
            writer.Write(IsSpouse ? 1 : 0);
            writer.Write(IsTutor ? 1 : 0);
            writer.Write(IsTradePartner ? 1 : 0);
            return writer.ToArray();
        }
    }
}