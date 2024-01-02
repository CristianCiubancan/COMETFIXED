using System.Collections.Generic;

namespace Comet.Network.Packets.Game
{
    /// <remarks>Packet Type 1006</remarks>
    /// <summary>
    ///     Message defining character information, used to initialize the client interface
    ///     and game state. Character information is loaded from the game database on login
    ///     if a character exists.
    /// </summary>
    public abstract class MsgUserInfo<T> : MsgBase<T>
    {
        // Packet Properties
        public uint Identity { get; set; }
        public uint Mesh { get; set; }
        public ushort Hairstyle { get; set; }
        public uint Silver { get; set; }
        public uint Jewels { get; set; }
        public ulong Experience { get; set; }
        public ushort Strength { get; set; }
        public ushort Agility { get; set; }
        public ushort Vitality { get; set; }
        public ushort Spirit { get; set; }
        public ushort AttributePoints { get; set; }
        public ushort HealthPoints { get; set; }
        public ushort ManaPoints { get; set; }
        public ushort KillPoints { get; set; }
        public byte Level { get; set; }
        public byte CurrentClass { get; set; }
        public byte PreviousClass { get; set; }
        public byte Rebirths { get; set; }
        public byte FirstClass { get; set; }
        public uint QuizPoints { get; set; }
        public ushort EnlightenPoints { get; set; }
        public uint EnlightenExp { get; set; }
        public uint VipLevel { get; set; }
        public ushort UserTitle { get; set; }
        public string CharacterName { get; set; }
        public string SpouseName { get; set; }

        /// <summary>
        ///     Encodes the packet structure defined by this message class into a byte packet
        ///     that can be sent to the client. Invoked automatically by the client's send
        ///     method. Encodes using byte ordering rules interoperable with the game client.
        /// </summary>
        /// <returns>Returns a byte packet of the encoded packet.</returns>
        public override byte[] Encode()
        {
            var writer = new PacketWriter();
            writer.Write((ushort) Type);   // 2
            writer.Write(Identity);        // 4
            writer.Write(Mesh);            // 8
            writer.Write(Hairstyle);       // 12
            writer.Write(Silver);          // 14
            writer.Write(Jewels);          // 18
            writer.Write(Experience);      // 22
            writer.Write((ulong) 0);       // 30
            writer.Write((ulong) 0);       // 38
            writer.Write((uint) 0);        // 46
            writer.Write(Strength);        // 50
            writer.Write(Agility);         // 52
            writer.Write(Vitality);        // 54
            writer.Write(Spirit);          // 56
            writer.Write(AttributePoints); // 58
            writer.Write(HealthPoints);    // 60 
            writer.Write(ManaPoints);      // 62
            writer.Write(KillPoints);      // 64 
            writer.Write(Level);           // 66
            writer.Write(CurrentClass);    // 67
            writer.Write(PreviousClass);   // 68
            writer.Write(Rebirths);        // 69 
            writer.Write(FirstClass);      // 70
            writer.Write(QuizPoints);      // 71
            writer.Write(EnlightenPoints); // 75
            writer.Write(EnlightenExp);    // 77
            writer.Write((ushort) 0);      // 81
            writer.Write(VipLevel);        // 83
            writer.Write(UserTitle);       // 87
            writer.Write(new List<string>
            {
                CharacterName,
                SpouseName
            });
            return writer.ToArray();
        }
    }
}