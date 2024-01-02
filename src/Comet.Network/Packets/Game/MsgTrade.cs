namespace Comet.Network.Packets.Game
{
    public abstract class MsgTrade<T> : MsgBase<T>
    {
        public uint Data { get; set; }
        public TradeAction Action { get; set; }

        public override void Decode(byte[] bytes)
        {
            var reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType) reader.ReadUInt16();
            Data = reader.ReadUInt32();
            Action = (TradeAction) reader.ReadUInt32();
        }

        public override byte[] Encode()
        {
            var writer = new PacketWriter();
            writer.Write((ushort) PacketType.MsgTrade);
            writer.Write(Data);
            writer.Write((uint) Action);
            return writer.ToArray();
        }

        public enum TradeAction
        {
            Apply = 1,
            Quit,
            Open,
            Success,
            Fail,
            AddItem,
            AddMoney,
            ShowMoney,
            Accept = 10,
            AddItemFail,
            ShowConquerPoints,
            AddConquerPoints,
            Timeout = 17
        }
    }
}