using System;

namespace Comet.Network.Packets.Game
{
    public abstract class MsgInteract<T> : MsgBase<T>
    {
        public MsgInteract()
        {
            Timestamp = Environment.TickCount;
        }

        public int Timestamp { get; set; }
        public int Padding { get; set; }
        public uint SenderIdentity { get; set; }
        public uint TargetIdentity { get; set; }
        public ushort PosX { get; set; }
        public ushort PosY { get; set; }
        public MsgInteractType Action { get; set; }
        public int Data { get; set; }
        public int Command { get; set; }
        public InteractionEffect Effect { get; set; }
        public int EffectValue { get; set; }

        /// <summary>
        ///     Decodes a byte packet into the packet structure defined by this message class.
        ///     Should be invoked to structure data from the client for processing. Decoding
        ///     follows TQ Digital's byte ordering rules for an all-binary protocol.
        /// </summary>
        /// <param name="bytes">Bytes from the packet processor or client socket</param>
        public override void Decode(byte[] bytes)
        {
            var reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType) reader.ReadUInt16();
            Padding = reader.ReadInt32();
            SenderIdentity = reader.ReadUInt32();           // 8
            TargetIdentity = reader.ReadUInt32();           // 12
            PosX = reader.ReadUInt16();                     // 16
            PosY = reader.ReadUInt16();                     // 18
            Action = (MsgInteractType) reader.ReadUInt32(); // 20
            Data = reader.ReadInt32();                      // 24
            Command = reader.ReadInt32();                   // 28
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
            writer.Write((ushort) PacketType.MsgInteract);
            writer.Write(Padding);        // 8
            writer.Write(SenderIdentity); // 12
            writer.Write(TargetIdentity); // 16
            writer.Write(PosX);           // 20
            writer.Write(PosY);           // 22 
            writer.Write((uint) Action);  // 24
            writer.Write(Data);           // 28
            writer.Write(Command);        // 32
            return writer.ToArray();
        }
    }

    public enum MsgInteractType : uint
    {
        None = 0,
        Steal = 1,
        Attack = 2,
        Heal = 3,
        Poison = 4,
        Assassinate = 5,
        Freeze = 6,
        Unfreeze = 7,
        Court = 8,
        Marry = 9,
        Divorce = 10,
        PresentMoney = 11,
        PresentItem = 12,
        SendFlowers = 13,
        Kill = 14,
        JoinGuild = 15,
        AcceptGuildMember = 16,
        KickoutGuildMember = 17,
        PresentPower = 18,
        QueryInfo = 19,
        RushAttack = 20,
        Unknown21 = 21,
        AbortMagic = 22,
        ReflectWeapon = 23,
        MagicAttack = 24,
        Shoot5065 = 25,
        ReflectMagic = 26,
        Dash = 27,
        Shoot = 28,
        Quarry = 29,
        Chop = 30,
        Hustle = 31,
        Soul = 32,
        AcceptMerchant = 33,
        IncreaseJar = 36,
        PresentEmoney = 39,
        InitialMerchant = 40,
        CancelMerchant = 41,
        MerchantProgress = 42,
        CounterKill = 43,
        CounterKillSwitch = 44,
        FatalStrike = 45,
        CoupleActionRequest = 46,
        CoupleActionConfirm,
        CoupleActionRefuse,
        CoupleActionStart,
        CoupleActionEnd,
        AzureDmg = 55
    }

    [Flags]
    public enum InteractionEffect : ushort
    {
        None = 0x0,
        Block = 0x1,          // 1
        Penetration = 0x2,    // 2
        CriticalStrike = 0x4, // 4
        Breakthrough = 0x2,   // 8
        MetalResist = 0x10,   // 16
        WoodResist = 0x20,    // 32
        WaterResist = 0x40,   // 64
        FireResist = 0x80,    // 128
        EarthResist = 0x100
    }
}