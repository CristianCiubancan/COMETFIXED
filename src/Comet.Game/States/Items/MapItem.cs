using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Comet.Database.Entities;
using Comet.Game.Database;
using Comet.Game.Packets;
using Comet.Game.World;
using Comet.Game.World.Managers;
using Comet.Game.World.Maps;
using Comet.Shared;
using static Comet.Network.Packets.Game.MsgMapItem<Comet.Game.States.Client>;

namespace Comet.Game.States.Items
{
    public sealed class MapItem : Role
    {
        /// <summary>
        ///     Timer to keep the object alive in the map.
        /// </summary>
        private TimeOut m_tAlive = new();

        /// <summary>
        ///     Time to lock object so non-teammates cannot pick it up.
        /// </summary>
        private TimeOut mProtection = new();

        private MapItemInfo mInfo;
        private Item mItemInfo;
        private DbItemtype mItemType;

        public MapItem(uint idRole)
        {
            Identity = idRole;
        }

        public MapItem(uint idRole, DbItemDrop drop)
        {
            Identity = idRole;
            ItemDrop = drop;
        }

        public DbItemDrop ItemDrop { get; }
        public DropMode Mode { get; private set; } = DropMode.Common;

        #region Creation

        public bool Create(GameMap map, Point pos, MapItemInfo info, uint idOwner, DropMode mode)
        {
            if (map == null || info.Equals(default) || info.Type == 0) return false;
            mItemType = ItemManager.GetItemtype(info.Type);
            if (mItemType == null) return false;

            m_tAlive = new TimeOut(_DISAPPEAR_TIME);
            m_tAlive.Startup(_DISAPPEAR_TIME);

            mIdMap = map.Identity;
            Map = map;
            MapX = (ushort) pos.X;
            MapY = (ushort) pos.Y;

            mInfo = info;
            Name = mItemType.Name;

            if (idOwner != 0)
            {
                OwnerIdentity = idOwner;
                mProtection = new TimeOut(_MAPITEM_PRIV_SECS);
                mProtection.Startup(_MAPITEM_PRIV_SECS);
                mProtection.Update();
            }

            Mode = mode;
            return true;
        }

        public async Task<bool> CreateAsync(GameMap map, Point pos, uint idType, uint idOwner, byte nPlus, byte nDmg,
                                            short nDura, DropMode mode)
        {
            mItemType = ItemManager.GetItemtype(idType);
            return mItemType != null && await CreateAsync(map, pos, mItemType, idOwner, nPlus, nDmg, nDura, mode);
        }

        public async Task<bool> CreateAsync(GameMap map, Point pos, DbItemtype itemType, uint idOwner, byte nPlus,
                                            byte nDmg,
                                            short nDura, DropMode mode)
        {
            if (map == null || itemType == null) return false;

            m_tAlive = new TimeOut(_DISAPPEAR_TIME);
            m_tAlive.Startup(_DISAPPEAR_TIME);

            mIdMap = map.Identity;
            Map = map;
            MapX = (ushort) pos.X;
            MapY = (ushort) pos.Y;

            if (ItemDrop != null)
            {
                ItemDrop.X = MapX;
                ItemDrop.Y = MapY;
                await ServerDbContext.SaveAsync(ItemDrop);
            }

            mInfo.Addition = nPlus;
            mInfo.ReduceDamage = nDmg;

            ushort amount;
            var amountLimit = (ushort) Math.Max(1, itemType.AmountLimit * await Kernel.NextRateAsync(0.3d));
            if (itemType.Type % 10 > 5)
            {
                amount = (ushort) (amountLimit * (50 + await Kernel.NextAsync(50)) / 100);
            }
            else
            {
                var price = (int) Math.Max(1, itemType.Price);
                amount = (ushort) Math.Max(1, Math.Min(amountLimit, 3 * amountLimit * price / price));
            }

            mInfo.Durability = amount;
            mInfo.MaximumDurability = amountLimit;

            mInfo.Color = Item.ItemColor.Orange;

            mItemType = itemType;
            mInfo.Type = mItemType.Type;

            Name = mItemType.Name;

            if (idOwner != 0)
            {
                OwnerIdentity = idOwner;
                mProtection = new TimeOut(_MAPITEM_PRIV_SECS);
                mProtection.Startup(_MAPITEM_PRIV_SECS);
                mProtection.Update();
            }

            Mode = mode;
            return true;
        }

        public async Task<bool> CreateAsync(GameMap map, Point pos, Item pInfo, uint idOwner)
        {
            if (map == null || pInfo == null) return false;

            int nAliveSecs = _MAPITEM_USERMAX_ALIVESECS;
            if (pInfo.Itemtype != null)
                nAliveSecs = (int) (pInfo.Itemtype.Price / _MAPITEM_ALIVESECS_PERPRICE + _MAPITEM_USERMIN_ALIVESECS);

            if (nAliveSecs > _MAPITEM_USERMAX_ALIVESECS)
                nAliveSecs = _MAPITEM_USERMAX_ALIVESECS;

            m_tAlive = new TimeOut(nAliveSecs);
            m_tAlive.Update();

            mIdMap = map.Identity;
            Map = map;
            MapX = (ushort) pos.X;
            MapY = (ushort) pos.Y;

            if (ItemDrop != null)
            {
                ItemDrop.X = MapX;
                ItemDrop.Y = MapY;
                await ServerDbContext.SaveAsync(ItemDrop);
            }

            Name = pInfo.Itemtype?.Name ?? "";

            mItemInfo = pInfo;
            mInfo.Type = pInfo.Type;
            mInfo.Color = pInfo.Color;
            mItemInfo.OwnerIdentity = 0;
            await mItemInfo.ChangeOwnerAsync(0, Item.ChangeOwnerType.DropItem);
            mItemInfo.Position = Item.ItemPosition.Floor;
            return true;
        }

        public async Task<bool> CreateMoneyAsync(GameMap map, Point pos, uint dwMoney, uint idOwner, DropMode mode)
        {
            if (map == null || Identity == 0) return false;

            int nAliveSecs = _MAPITEM_MONSTER_ALIVESECS;
            if (idOwner == 0)
            {
                nAliveSecs = (int) (dwMoney / _MAPITEM_ALIVESECS_PERPRICE + _MAPITEM_USERMIN_ALIVESECS);
                if (nAliveSecs > _MAPITEM_USERMAX_ALIVESECS)
                    nAliveSecs = _MAPITEM_USERMAX_ALIVESECS;
            }

            m_tAlive = new TimeOut(nAliveSecs);
            m_tAlive.Update();

            mIdMap = map.Identity;
            Map = map;
            MapX = (ushort) pos.X;
            MapY = (ushort) pos.Y;

            if (ItemDrop != null)
            {
                ItemDrop.X = MapX;
                ItemDrop.Y = MapY;
                await ServerDbContext.SaveAsync(ItemDrop);
            }

            uint idType;
            if (dwMoney < _ITEM_SILVER_MAX)
                idType = 1090000;
            else if (dwMoney < _ITEM_SYCEE_MAX)
                idType = 1090010;
            else if (dwMoney < _ITEM_GOLD_MAX)
                idType = 1090020;
            else if (dwMoney < _ITEM_GOLDBULLION_MAX)
                idType = 1091000;
            else if (dwMoney < _ITEM_GOLDBAR_MAX)
                idType = 1091010;
            else
                idType = 1091020;

            Money = dwMoney;

            mInfo.Type = idType;

            if (idOwner != 0)
            {
                OwnerIdentity = idOwner;
                mProtection = new TimeOut(_MAPITEM_PRIV_SECS);
                mProtection.Startup(_MAPITEM_PRIV_SECS);
                mProtection.Update();
            }

            Mode = mode;
            return true;
        }

        #endregion

        #region Identity

        public uint ItemIdentity => mItemInfo?.Identity ?? 0;

        public uint Itemtype
        {
            get => mInfo.Type;
            private set => mInfo.Type = value;
        }

        public uint OwnerIdentity { get; private set; }

        public uint Money { get; private set; }

        public bool IsPrivate()
        {
            return mProtection.IsActive() && !mProtection.IsTimeOut();
        }

        public bool IsMoney()
        {
            return Money > 0;
        }

        public bool IsJewel()
        {
            return false;
        }

        public bool IsItem()
        {
            return !IsMoney() && !IsJewel();
        }

        public bool IsConquerPointsPack()
        {
            return Itemtype == 729910 || Itemtype == 729911 || Itemtype == 729912;
        }

        public MapItemInfo Info => mInfo;

        #endregion

        #region Generation

        public async Task<Item> GetInfoAsync(Character owner)
        {
            if (mItemType == null && mItemInfo == null)
                return null;

            if (mItemInfo == null)
            {
                mItemInfo = new Item(owner);

                await mItemInfo.CreateAsync(mItemType);

                mItemInfo.Color = mInfo.Color;

                mItemInfo.ChangeAddition(mInfo.Addition);
                mItemInfo.ReduceDamage = mInfo.ReduceDamage;

                if (mInfo.SocketNum > 0)
                    mItemInfo.SocketOne = Item.SocketGem.EmptySocket;
                if (mInfo.SocketNum > 1)
                    mItemInfo.SocketTwo = Item.SocketGem.EmptySocket;

                mItemInfo.Durability = mInfo.Durability;
                mItemInfo.MaximumDurability = mInfo.MaximumDurability;
            }

            mItemInfo.Position = Item.ItemPosition.Inventory;

            if (Mode.HasFlag(DropMode.Bound))
                mItemInfo.IsBound = true;

            await mItemInfo.ChangeOwnerAsync(owner.Identity, Item.ChangeOwnerType.PickupItem);
            await mItemInfo.SaveAsync();
            return mItemInfo;
        }

        #endregion

        #region Battle

        public override bool IsImmunity(Role target)
        {
            return true;
        }

        #endregion

        #region Map

        public void SetAliveTimeout(int durationSecs)
        {
            m_tAlive.Startup(durationSecs);
        }

        public bool CanDisappear()
        {
            return m_tAlive.IsTimeOut();
        }

        public async Task DisappearAsync()
        {
            if (mItemInfo != null)
                await mItemInfo.DeleteAsync(Item.ChangeOwnerType.DeleteDroppedItem);

            await LeaveMapAsync();
        }

        public override async Task EnterMapAsync()
        {
            Map = MapManager.GetMap(MapIdentity);
            if (Map != null)
                await Map.AddAsync(this);
        }

        public override async Task LeaveMapAsync()
        {
            IdentityGenerator.MapItem.ReturnIdentity(Identity);
            if (Map != null)
            {
                var msg = new MsgMapItem
                {
                    Identity = Identity,
                    MapX = MapX,
                    MapY = MapY,
                    Itemtype = Itemtype,
                    Mode = DropType.DisappearItem
                };
                await Map.BroadcastRoomMsgAsync(MapX, MapY, msg);
                await Map.RemoveAsync(Identity);
                RoleManager.RemoveRole(Identity);
            }

            Map = null;
        }

        #endregion

        #region OnTimer

        public override Task OnTimerAsync()
        {
            if (CanDisappear())
                QueueAction(async () => { await DisappearAsync(); });
            return Task.CompletedTask;
        }

        #endregion

        #region Socket

        public override async Task SendSpawnToAsync(Character player)
        {
            if (Mode.HasFlag(DropMode.OnlyOwner) && player.Identity != OwnerIdentity)
                return;

            await player.SendAsync(new MsgMapItem
            {
                Identity = Identity,
                MapX = MapX,
                MapY = MapY,
                Itemtype = Itemtype,
                Mode = DropType.LayItem,
                Color = (ushort) mInfo.Color
            });
        }

        #endregion

        #region Constants

        private const uint _ITEM_SILVER_MIN = 1;
        private const uint _ITEM_SILVER_MAX = 9;
        private const uint _ITEM_SYCEE_MIN = 10;
        private const uint _ITEM_SYCEE_MAX = 99;
        private const uint _ITEM_GOLD_MIN = 100;
        private const uint _ITEM_GOLD_MAX = 999;
        private const uint _ITEM_GOLDBULLION_MIN = 1000;
        private const uint _ITEM_GOLDBULLION_MAX = 1999;
        private const uint _ITEM_GOLDBAR_MIN = 2000;
        private const uint _ITEM_GOLDBAR_MAX = 4999;
        private const uint _ITEM_GOLDBARS_MIN = 5000;
        private const uint _ITEM_GOLDBARS_MAX = 10000000;

        private const int _PICKUP_TIME = 30;
        private const int _DISAPPEAR_TIME = 60;
        private const int _MAPITEM_ONTIMER_SECS = 5;
        private const int _MAPITEM_MONSTER_ALIVESECS = 60;
        private const int _MAPITEM_USERMAX_ALIVESECS = 90;
        private const int _MAPITEM_USERMIN_ALIVESECS = 60;

        private const int _MAPITEM_ALIVESECS_PERPRICE =
            1000 / (_MAPITEM_USERMAX_ALIVESECS - _MAPITEM_USERMIN_ALIVESECS);

        private const int _MAPITEM_PRIV_SECS = 30;
        private const int _PICKMAPITEMDIST_LIMIT = 0;

        #endregion

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto, Pack = 2, Size = 12)]
        public struct MapItemInfo
        {
            public uint Type { get; set; }
            public ushort Durability { get; set; }
            public ushort MaximumDurability { get; set; }
            public byte ReduceDamage { get; set; }
            public byte Addition { get; set; }
            public byte SocketNum { get; set; }
            public Item.ItemColor Color { get; set; }
        }

        [Flags]
        public enum DropMode
        {
            Common,
            Bound,
            OnlyOwner
        }
    }
}