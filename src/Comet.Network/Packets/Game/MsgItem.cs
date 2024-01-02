using System;

namespace Comet.Network.Packets.Game
{
    /// <remarks>Packet Type 1009</remarks>
    /// <summary>
    ///     Message containing an item action command. Item actions are usually performed to
    ///     manage player equipment, inventory, money, or item shop purchases and sales. It
    ///     is serves a second purpose for measuring client ping.
    /// </summary>
    public abstract class MsgItem<T> : MsgBase<T>
    {
        public MsgItem()
        {
        }

        public MsgItem(uint identity, ItemActionType action, uint cmd = 0, uint param = 0)
        {
            Identity = identity;
            Command = cmd;
            Action = action;
            Timestamp = (uint) Environment.TickCount;
            Argument = param;
        }

        // Packet Properties
        public uint Identity { get; set; }
        public uint Command { get; set; }
        public uint Timestamp { get; set; }
        public uint Argument { get; set; }
        public ItemActionType Action { get; set; }
        public uint Argument2 { get; set; }

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
            Identity = reader.ReadUInt32();
            Command = reader.ReadUInt32();
            Action = (ItemActionType) reader.ReadUInt32();
            Timestamp = reader.ReadUInt32();
            Argument = reader.ReadUInt32();
            Argument2 = reader.ReadUInt32();
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
            writer.Write((ushort) PacketType.MsgItem);
            writer.Write(Identity);
            writer.Write(Command);
            writer.Write((uint) Action);
            writer.Write(Timestamp);
            writer.Write(Argument);
            writer.Write(Argument2);
            return writer.ToArray();
        }

        /// <summary>
        ///     Enumeration type for defining item actions that may be requested by the user,
        ///     or given to by the server. Allows for action handling as a packet subtype.
        ///     Enums should be named by the action they provide to a system in the context
        ///     of the player item.
        /// </summary>
        public enum ItemActionType
        {
            ShopPurchase = 1,
            ShopSell,
            InventoryRemove,
            InventoryEquip,
            EquipmentWear,
            EquipmentRemove,
            EquipmentSplit,
            EquipmentCombine,
            BankQuery,
            BankDeposit,
            BankWithdraw,

            //InventoryDropSilver,
            EquipmentRepair = 14,
            EquipmentRepairAll,
            EquipmentImprove = 19,
            EquipmentLevelUp,
            BoothQuery,
            BoothSell,
            BoothRemove,
            BoothPurchase,
            EquipmentAmount,
            Fireworks,
            ClientPing = 27,
            EquipmentEnchant,
            BoothSellPoints,
            RedeemEquipment = 32,
            DetainEquipment = 33,
            DetainRewardClose = 34,
            TalismanProgress = 35,
            TalismanProgressEmoney = 36,
            InventoryDropItem = 37,
            InventoryDropSilver = 38
        }

        public enum Moneytype
        {
            Silver,
            ConquerPoints,

            /// <summary>
            ///     CPs(B)
            /// </summary>
            ConquerPointsMono
        }
    }
}