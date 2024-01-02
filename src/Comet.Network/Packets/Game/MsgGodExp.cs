namespace Comet.Network.Packets.Game
{
    public abstract class MsgGodExp<T> : MsgBase<T>
    {
        public MsgGodExpAction Action;
        public int GodTimeExp;
        public int HuntExp;

        public override void Decode(byte[] bytes)
        {
            var reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType) reader.ReadUInt16();
            GodTimeExp = reader.ReadInt32();
            HuntExp = reader.ReadInt32();
        }

        public override byte[] Encode()
        {
            var writer = new PacketWriter();
            writer.Write((ushort) PacketType.MsgGodExp);
            writer.Write((uint) Action);
            writer.Write(GodTimeExp);
            writer.Write(HuntExp);
            return writer.ToArray();
        }

        public enum MsgGodExpAction
        {
            Query,
            ClaimOnlineTraining,
            ClaimHuntTraining
        }
    }
}