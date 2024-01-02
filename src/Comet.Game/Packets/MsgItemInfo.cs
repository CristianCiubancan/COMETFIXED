using Comet.Game.States;
using Comet.Game.States.Items;
using Comet.Network.Packets;
using Comet.Network.Packets.Game;

namespace Comet.Game.Packets
{
    public sealed class MsgItemInfo : MsgItemInfo<Client>
    {
        public MsgItemInfo(Item item, ItemMode mode = ItemMode.Default)
        {
            Type = PacketType.MsgItemInfo;

            if (mode == ItemMode.View)
                Identity = item.PlayerIdentity;
            else
                Identity = item.Identity;

            Itemtype = item.Type;
            Amount = item.Durability;
            AmountLimit = item.MaximumDurability;
            Mode = mode;
            Position = (ushort) item.Position;
            SocketProgress = item.SocketProgress;
            SocketOne = (byte) item.SocketOne;
            SocketTwo = (byte) item.SocketTwo;
            Effect = (byte) item.Effect;

            if (item.GetItemSubType() == 730)
                Plus = (byte) (item.Type % 100);
            else
                Plus = item.Plus;

            IsSuspicious = item.IsSuspicious();
            Bless = (byte) item.Blessing;
            Enchantment = item.Enchantment;
            Color = (byte) item.Color;
            IsLocked = item.IsLocked() || item.IsUnlocking();
            IsBound = item.IsBound;
            CompositionProgress = item.CompositionProgress;
            Inscribed = item.SyndicateIdentity != 0;
            AntiMonster = item.AntiMonster;
        }
    }
}