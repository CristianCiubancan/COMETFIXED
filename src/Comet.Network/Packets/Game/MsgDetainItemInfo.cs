namespace Comet.Network.Packets.Game
{
    public abstract class MsgDetainItemInfo<T> : MsgBase<T>
    {
        public uint Identity { get; set; }
        public uint ItemIdentity { get; set; }
        public uint ItemType { get; set; }
        public ushort Amount { get; set; }
        public ushort AmountLimit { get; set; }
        public Mode Action { get; set; }
        public uint SocketProgress { get; set; }
        public byte SocketOne { get; set; }
        public byte SocketTwo { get; set; }
        public ushort Effect { get; set; }
        public byte Addition { get; set; }
        public byte Blessing { get; set; }
        public bool Bound { get; set; }
        public byte Enchantment { get; set; }
        public bool Suspicious { get; set; }
        public bool Locked { get; set; }
        public byte Color { get; set; }
        public uint OwnerIdentity { get; set; }
        public string OwnerName { get; set; }
        public uint TargetIdentity { get; set; }
        public string TargetName { get; set; }
        public int DetainDate { get; set; }
        public bool Expired { get; set; }
        public int Cost { get; set; }
        public int RemainingDays { get; set; }

        public override byte[] Encode()
        {
            PacketWriter writer = new();
            writer.Write((ushort) PacketType.MsgDetainItemInfo);
            writer.Write(Identity);                      // 4
            writer.Write(ItemIdentity);                  // 8
            writer.Write(ItemType);                      // 12
            writer.Write(Amount);                        // 16
            writer.Write(AmountLimit);                   // 18
            writer.Write((int) Action);                  // 20
            writer.Write(SocketProgress);                // 24
            writer.Write(SocketOne);                     // 28
            writer.Write(SocketTwo);                     // 29
            writer.Write(new byte[2]);                   // 30
            writer.Write(Addition);                      // 32
            writer.Write(Blessing);                      // 33
            writer.Write(Bound);                         // 34 
            writer.Write(Enchantment);                   // 35
            writer.Write(0);                             // 36
            writer.Write((ushort) (Suspicious ? 1 : 0)); // 40
            writer.Write((ushort) (Locked ? 1 : 0));     // 42
            writer.Write((int) Color);                   // 44
            writer.Write(OwnerIdentity);                 // 48
            writer.Write(OwnerName, 16);                 // 52
            writer.Write(TargetIdentity);                // 68
            writer.Write(TargetName, 16);                // 72
            writer.Write(new byte[8]);
            writer.Write(Cost);            // 100
            writer.Write(Expired ? 1 : 0); // 104
            writer.Write(DetainDate);      // 108
            writer.Write(RemainingDays);   // 112
            return writer.ToArray();
        }

        public const int MAX_REDEEM_DAYS = 7;
        public const int MAX_REDEEM_SECONDS = 60 * 60 * 24 * MAX_REDEEM_DAYS;

        public enum Mode
        {
            DetainPage,
            ClaimPage,
            ReadyToClaim
        }
    }
}