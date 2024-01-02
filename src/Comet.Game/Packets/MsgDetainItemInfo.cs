using System;
using Comet.Database.Entities;
using Comet.Game.States;
using Comet.Game.States.Items;
using Comet.Network.Packets.Game;
using Comet.Shared;

namespace Comet.Game.Packets
{
    public sealed class MsgDetainItemInfo : MsgDetainItemInfo<Client>
    {
        public MsgDetainItemInfo(DbDetainedItem dbDetainItem, Item item, Mode mode)
        {
            Identity = dbDetainItem.Identity;
            ItemIdentity = item?.Identity ?? 0;
            ItemType = item?.Type ?? 0;
            Amount = item?.Durability ?? 0;
            AmountLimit = item?.MaximumDurability ?? 0;
            Action = mode;
            SocketProgress = item?.SocketProgress ?? 0;
            SocketOne = (byte) (item?.SocketOne ?? Item.SocketGem.NoSocket);
            SocketTwo = (byte) (item?.SocketTwo ?? Item.SocketGem.NoSocket);
            Effect = (ushort) (item?.Effect ?? Item.ItemEffect.None);
            Addition = item?.Plus ?? 0;
            Blessing = (byte) (item?.Blessing ?? 0);
            Bound = item?.IsBound ?? false;
            Enchantment = item?.Enchantment ?? 0;
            Suspicious = item?.IsSuspicious() ?? false;
            Locked = item?.IsLocked() ?? false;
            Color = (byte) (item?.Color ?? Item.ItemColor.Orange);
            if (Action == Mode.ClaimPage)
            {
                OwnerIdentity = dbDetainItem.HunterIdentity;
                OwnerName = dbDetainItem.HunterName;
                TargetIdentity = dbDetainItem.TargetIdentity;
                TargetName = dbDetainItem.TargetName;
            }
            else if (Action == Mode.DetainPage)
            {
                OwnerIdentity = dbDetainItem.TargetIdentity;
                OwnerName = dbDetainItem.TargetName;
                TargetIdentity = dbDetainItem.HunterIdentity;
                TargetName = dbDetainItem.HunterName;
            }

            DetainDate = int.Parse(UnixTimestamp.ToDateTime(dbDetainItem.HuntTime).ToString("yyyyMMdd"));
            Cost = dbDetainItem.RedeemPrice;
            if (item != null)
            {
                Expired = long.Parse(DateTime.Now.ToString("yyyyMMddHHmmss")) -
                          long.Parse(UnixTimestamp.ToDateTime(dbDetainItem.HuntTime).ToString("yyyyMMddHHmmss")) >
                          MAX_REDEEM_DAYS * 1000000;
                RemainingDays =
                    Math.Max(
                        0,
                        int.Parse(DateTime.Now.ToString("yyyyMMdd")) -
                        int.Parse(UnixTimestamp.ToDateTime(dbDetainItem.HuntTime).ToString("yyyyMMdd")));
            }
            else
            {
                Expired = true;
                RemainingDays = 0;
            }
        }
    }
}