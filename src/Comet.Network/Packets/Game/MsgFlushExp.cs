﻿namespace Comet.Network.Packets.Game
{
    public abstract class MsgFlushExp<T> : MsgBase<T>
    {
        public uint Experience { get; set; }
        public ushort Identity { get; set; }
        public FlushMode Action { get; set; }

        public override void Decode(byte[] bytes)
        {
            var reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType) reader.ReadUInt16();
            Experience = reader.ReadUInt32();
            Identity = reader.ReadUInt16();
            Action = (FlushMode) reader.ReadUInt16();
        }

        public override byte[] Encode()
        {
            var writer = new PacketWriter();
            writer.Write((ushort) PacketType.MsgFlushExp);
            writer.Write(Experience);
            writer.Write(Identity);
            writer.Write((ushort) Action);
            return writer.ToArray();
        }

        public enum FlushMode : ushort
        {
            WeaponSkill,
            Magic,
            Skill
        }
    }
}