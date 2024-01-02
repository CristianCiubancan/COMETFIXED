using System.Collections.Generic;

namespace Comet.Network.Packets.Game
{
    public abstract class MsgSyncAction<T> : MsgBase<T>
    {
        public SyncAction Action { get; set; }
        public ushort X { get; set; }
        public ushort Y { get; set; }
        public int Count { get; set; }
        public List<uint> Targets { get; set; } = new();

        /// <inheritdoc />
        public override byte[] Encode()
        {
            PacketWriter writer = new();
            writer.Write((ushort) PacketType.MsgSyncAction);
            writer.Write((ushort) Action);
            writer.Write(X);
            writer.Write(Y);
            writer.Write((ushort) 0);
            writer.Write(Count = Targets.Count);
            foreach (uint target in Targets) writer.Write(target);
            return writer.ToArray();
        }
    }

    public enum SyncAction
    {
        Walk = 1,
        Jump
    }
}