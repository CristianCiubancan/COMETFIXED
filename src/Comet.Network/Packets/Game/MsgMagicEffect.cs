using System.Collections.Generic;

namespace Comet.Network.Packets.Game
{
    public abstract class MsgMagicEffect<T> : MsgBase<T>
    {
        private readonly List<MagicTarget> Targets = new();
        public uint AttackerIdentity { get; set; }
        public ushort MapX { get; set; }
        public ushort MapY { get; set; }
        public ushort MagicIdentity { get; set; }
        public ushort MagicLevel { get; set; }
        public ushort Count { get; set; }

        public void Append(uint idTarget, int damage, bool showValue)
        {
            Count++;
            Targets.Add(new MagicTarget
            {
                Identity = idTarget,
                Damage = damage,
                Show = showValue ? 1 : 0
            });
        }

        public void ClearTargets()
        {
            Count = 0;
            Targets.Clear();
        }

        /// <summary>
        ///     Encodes the packet structure defined by this message class into a byte packet
        ///     that can be sent to the client. Invoked automatically by the client's send
        ///     method. Encodes using byte ordering rules interoperable with the game client.
        /// </summary>
        /// <returns>Returns a byte packet of the encoded packet.</returns>
        public override byte[] Encode()
        {
            var writer = new PacketWriter();
            writer.Write((ushort) PacketType.MsgMagicEffect);
            writer.Write(AttackerIdentity);
            writer.Write(MapX);
            writer.Write(MapY);
            writer.Write(MagicIdentity);
            writer.Write(MagicLevel);
            writer.Write((uint) Count);
            foreach (MagicTarget target in Targets)
            {
                writer.Write(target.Identity);
                writer.Write(target.Damage);
                writer.Write(target.Show);
            }

            return writer.ToArray();
        }

        private struct MagicTarget
        {
            public uint Identity;
            public int Damage;
            public int Show;
        }
    }
}