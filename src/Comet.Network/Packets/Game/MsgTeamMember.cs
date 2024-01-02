using System.Collections.Generic;

namespace Comet.Network.Packets.Game
{
    public abstract class MsgTeamMember<T> : MsgBase<T>
    {
        public List<TeamMember> Members = new();

        public byte Action { get; set; }
        public byte Count { get; set; }
        public byte Unknown0 { get; set; }
        public byte Unknown1 { get; set; }

        /// <summary>
        ///     Encodes the packet structure defined by this message class into a byte packet
        ///     that can be sent to the client. Invoked automatically by the client's send
        ///     method. Encodes using byte ordering rules interoperable with the game client.
        /// </summary>
        /// <returns>Returns a byte packet of the encoded packet.</returns>
        public override byte[] Encode()
        {
            var writer = new PacketWriter();
            writer.Write((ushort) PacketType.MsgTeamMember);
            writer.Write(Action);
            writer.Write((byte) Members.Count);
            writer.Write(Unknown0);
            writer.Write(Unknown1);
            foreach (TeamMember member in Members)
            {
                writer.Write(member.Name, 16);
                writer.Write(member.Identity);
                writer.Write(member.Lookface);
                writer.Write(member.MaxLife);
                writer.Write(member.Life);
            }

            return writer.ToArray();
        }

        public struct TeamMember
        {
            public string Name { get; set; }
            public uint Identity { get; set; }
            public uint Lookface { get; set; }
            public ushort MaxLife { get; set; }
            public ushort Life { get; set; }
        }

        public const byte ADD_MEMBER_B = 0, DEL_MEMBER_B = 1;
    }
}