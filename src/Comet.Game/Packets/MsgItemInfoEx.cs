using Comet.Game.States;
using Comet.Game.States.Items;
using Comet.Network.Packets;
using Comet.Network.Packets.Game;

namespace Comet.Game.Packets
{
    public sealed class MsgItemInfoEx : MsgItemInfoEx<Client>
    {
        public MsgItemInfoEx(BoothItem item)
        {
            Type = PacketType.MsgItemInfoEx;

            Identity = item.Identity;
            TargetIdentity = item.Item.PlayerIdentity;
            ItemType = item.Item.Type;
            Amount = item.Item.Durability;
            AmountLimit = item.Item.MaximumDurability;
            Position = (ushort) Item.ItemPosition.Inventory;
            SocketOne = (byte) item.Item.SocketOne;
            SocketTwo = (byte) item.Item.SocketTwo;
            Addition = item.Item.Plus;
            Blessing = (byte) item.Item.Blessing;
            Enchantment = item.Item.Enchantment;
            Color = (byte) item.Item.Color;
            Mode = item.IsSilver ? ViewMode.Silvers : ViewMode.Emoney;
            Price = item.Value;
            SocketProgress = item.Item.SocketProgress;
            CompositionProgress = item.Item.CompositionProgress;
        }
    }
}