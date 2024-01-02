using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Comet.Game.Packets;
using Comet.Game.States.Items;
using Comet.Game.World.Maps;
using Comet.Network.Packets.Game;

namespace Comet.Game.States.Npcs
{
    public class BoothNpc : BaseNpc
    {
        private Npc m_ownerNpc;
        private Character mOwner;
        private readonly ConcurrentDictionary<uint, BoothItem> m_items = new();

        public BoothNpc(Character owner)
            : base(owner.Identity % 1000000 + owner.Identity / 1000000 * 100000)
        {
            mOwner = owner;
        }

        public override async Task<bool> InitializeAsync()
        {
            m_ownerNpc =
                mOwner.Screen.Roles.Values.FirstOrDefault(
                    x => x is Npc && x.MapX == mOwner.MapX - 2 && x.MapY == mOwner.MapY) as Npc;
            if (m_ownerNpc == null)
                return false;

            mIdMap = mOwner.MapIdentity;
            mPosX = (ushort) (mOwner.MapX + 1);
            mPosY = mOwner.MapY;

            Mesh = 406;
            Name = $"{mOwner.Name}Stash";

            await mOwner.SetDirectionAsync(FacingDirection.SouthEast);
            await mOwner.SetActionAsync(EntityAction.Sit);

            return await base.InitializeAsync();
        }

        public override ushort Type => BOOTH_NPC;

        public string HawkMessage { get; set; }

        #region Items management

        public async Task QueryItemsAsync(Character requester)
        {
            if (GetDistance(requester) > Screen.VIEW_SIZE)
                return;

            foreach (BoothItem item in m_items.Values)
            {
                if (!ValidateItem(item.Identity))
                {
                    m_items.TryRemove(item.Identity, out _);
                    continue;
                }

                await requester.SendAsync(new MsgItemInfoEx(item) {TargetIdentity = Identity});
            }
        }

        public bool AddItem(Item item, uint value, MsgItem<Client>.Moneytype type)
        {
            var boothItem = new BoothItem();
            if (!boothItem.Create(item, Math.Min(value, int.MaxValue), type == MsgItem<Client>.Moneytype.Silver))
                return false;
            return m_items.TryAdd(boothItem.Identity, boothItem);
        }

        public BoothItem QueryItem(uint idItem)
        {
            return m_items.Values.FirstOrDefault(x => x.Identity == idItem);
        }

        public bool RemoveItem(uint idItem)
        {
            return m_items.TryRemove(idItem, out _);
        }

        public bool ValidateItem(uint id)
        {
            Item item = mOwner.UserPackage[id];
            if (item == null)
                return false;
            if (item.IsBound)
                return false;
            if (item.IsLocked())
                return false;
            if (item.IsSuspicious())
                return false;
            return true;
        }

        #endregion

        #region Enter and Leave Map

        public override Task EnterMapAsync()
        {
            return base.EnterMapAsync();
        }

        public override async Task LeaveMapAsync()
        {
            if (m_ownerNpc != null)
            {
                await mOwner.SetActionAsync(EntityAction.Stand);
                mOwner = null;
                m_ownerNpc = null;
            }

            m_items.Clear();
            await base.LeaveMapAsync();
        }

        #endregion

        #region Socket

        public override async Task SendSpawnToAsync(Character player)
        {
            await player.SendAsync(new MsgNpcInfoEx(this));

            if (!string.IsNullOrEmpty(HawkMessage))
                await player.SendAsync(new MsgTalk(mOwner.Identity, TalkChannel.Vendor, Color.White,
                                                   HawkMessage));
        }

        #endregion
    }
}