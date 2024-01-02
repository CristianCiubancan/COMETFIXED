using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Comet.Core;
using Comet.Database.Entities;
using Comet.Game.Database;
using Comet.Game.Packets;
using Comet.Game.States.Items;
using Comet.Network.Packets.Game;

namespace Comet.Game.States
{
    public sealed class Trade
    {
        private const int MaxTradeItems = 20;
        private const int MaxTradeMoney = 1000000000;
        private const int MaxTradeEmoney = 1000000000;

        private readonly ConcurrentDictionary<uint, Item> mDicItems1 = new();
        private readonly ConcurrentDictionary<uint, Item> mDicItems2 = new();

        private uint mMoney1, mMoney2;
        private uint mEmoney1, mEmoney2;

        private bool mAccept1, mAccept2;

        public Trade(Character p1, Character p2)
        {
            User1 = p1;
            User2 = p2;

            User1.Trade = this;
            User2.Trade = this;
        }

        public Character User1 { get; }
        public Character User2 { get; }

        public bool Accepted => mAccept1 && mAccept2;

        public async Task<bool> AddItemAsync(uint idItem, Character sender)
        {
            if (sender.Identity != User1.Identity
                && sender.Identity != User2.Identity)
                return false;

            Character target = sender.Identity == User1.Identity ? User2 : User1;
            ConcurrentDictionary<uint, Item> items = User1.Identity == sender.Identity ? mDicItems1 : mDicItems2;

            Item item = sender.UserPackage[idItem];
            if (item == null)
            {
                await sender.SendAsync(Language.StrNotToTrade);
                await sender.SendAsync(RemoveMsg(idItem));
                return false;
            }

            if (items.ContainsKey(idItem))
            {
                await sender.SendAsync(RemoveMsg(idItem));
                return false;
            }

            if (!sender.IsPm())
            {
                if (item.IsMonopoly() || item.IsBound)
                {
                    await sender.SendAsync(Language.StrNotToTrade);
                    await sender.SendAsync(RemoveMsg(idItem));
                    return false;
                }

                if (item.IsSuspicious())
                {
                    await sender.SendAsync(Language.StrNotToTrade);
                    await sender.SendAsync(RemoveMsg(idItem));
                    return false;
                }

                if (item.IsLocked() && !sender.IsValidTradePartner(target.Identity))
                {
                    await sender.SendAsync(Language.StrNotToTrade);
                    await sender.SendAsync(RemoveMsg(idItem));
                    return false;
                }
            }

            if (item.SyndicateIdentity != 0)
            {
                await sender.SendAsync(Language.StrNotToTrade);
                await sender.SendAsync(RemoveMsg(idItem));
                return false;
            }

            if (sender.Booth?.QueryItem(item.Identity) != null)
            {
                await sender.SendAsync(Language.StrNotToTrade);
                await sender.SendAsync(RemoveMsg(idItem));
                return false;
            }

            if (items.Count >= MaxTradeItems)
            {
                await sender.SendAsync(Language.StrTradeSashFull);
                await sender.SendAsync(RemoveMsg(idItem));
                return false;
            }

            if (!target.UserPackage.IsPackSpare(1))
            {
                await target.SendAsync(Language.StrTradeYourBagIsFull);
                await sender.SendAsync(Language.StrTradeTargetBagIsFull);
                await sender.SendAsync(RemoveMsg(idItem));
                return false;
            }

            items.TryAdd(item.Identity, item);
            await target.SendAsync(new MsgItemInfo(item, MsgItemInfo<Client>.ItemMode.Trade));
            return true;
        }

        public async Task<bool> AddMoneyAsync(uint amount, Character sender)
        {
            if (sender.Identity != User1.Identity
                && sender.Identity != User2.Identity)
                return false;

            Character target = sender.Identity == User1.Identity ? User2 : User1;

            if (amount > MaxTradeMoney)
            {
                await sender.SendAsync(string.Format(Language.StrTradeMuchMoney, MaxTradeMoney));
                await SendCloseAsync();
                return false;
            }

            if (sender.Silvers < amount)
            {
                await sender.SendAsync(Language.StrNotEnoughMoney);
                await SendCloseAsync();
                return false;
            }

            if (sender.Identity == User1.Identity)
                mMoney1 = amount;
            else
                mMoney2 = amount;

            await target.SendAsync(new MsgTrade
            {
                Data = amount,
                Action = MsgTrade<Client>.TradeAction.ShowMoney
            });
            return true;
        }

        public async Task<bool> AddEmoneyAsync(uint amount, Character sender)
        {
            if (sender.Identity != User1.Identity
                && sender.Identity != User2.Identity)
                return false;

            Character target = sender.Identity == User1.Identity ? User2 : User1;

            if (amount > MaxTradeEmoney)
            {
                await sender.SendAsync(string.Format(Language.StrTradeMuchEmoney, MaxTradeEmoney));
                await SendCloseAsync();
                return false;
            }

            if (sender.ConquerPoints < amount)
            {
                await sender.SendAsync(Language.StrNotEnoughMoney);
                await SendCloseAsync();
                return false;
            }

            if (sender.Identity == User1.Identity)
                mEmoney1 = amount;
            else
                mEmoney2 = amount;

            await target.SendAsync(new MsgTrade
            {
                Data = amount,
                Action = MsgTrade<Client>.TradeAction.ShowConquerPoints
            });
            return true;
        }

        public async Task AcceptAsync(uint acceptId)
        {
            if (acceptId == User1.Identity)
            {
                mAccept1 = true;
                await User2.SendAsync(new MsgTrade
                {
                    Action = MsgTrade<Client>.TradeAction.Accept,
                    Data = acceptId
                });
            }
            else if (acceptId == User2.Identity)
            {
                mAccept2 = true;
                await User1.SendAsync(new MsgTrade
                {
                    Action = MsgTrade<Client>.TradeAction.Accept,
                    Data = acceptId
                });
            }

            if (!Accepted)
                return;

            bool success1 = mDicItems1.Values.All(x => User1.UserPackage[x.Identity] != null && !x.IsBound && !x.IsMonopoly());
            bool success2 = mDicItems2.Values.All(x => User2.UserPackage[x.Identity] != null && !x.IsBound && !x.IsMonopoly());

            bool success = success1 && success2;

            if (!User1.UserPackage.IsPackSpare(mDicItems2.Count))
                success = false;
            if (!User2.UserPackage.IsPackSpare(mDicItems1.Count))
                success = false;

            if (mMoney1 > User1.Silvers || mEmoney1 > User1.ConquerPoints)
                success = false;
            if (mMoney2 > User2.Silvers || mEmoney2 > User2.ConquerPoints)
                success = false;

            if (!success)
            {
                await SendCloseAsync();
                return;
            }

            var dbTrade = new DbTrade
            {
                Type = DbTrade.TradeType.Trade,
                UserIpAddress = User1.Client.IpAddress,
                UserMacAddress = User1.Client.MacAddress,
                TargetIpAddress = User2.Client.IpAddress,
                TargetMacAddress = User2.Client.MacAddress,
                MapIdentity = User1.MapIdentity,
                TargetEmoney = mEmoney2,
                TargetMoney = mMoney2,
                UserEmoney = mEmoney1,
                UserMoney = mMoney1,
                TargetIdentity = User2.Identity,
                UserIdentity = User1.Identity,
                TargetX = User2.MapX,
                TargetY = User2.MapY,
                UserX = User1.MapX,
                UserY = User1.MapY,
                Timestamp = DateTime.Now
            };
            await ServerDbContext.SaveAsync(dbTrade);

            await SendCloseAsync();

            await User1.SpendMoneyAsync((int) mMoney1);
            await User2.AwardMoneyAsync((int) mMoney1);

            await User2.SpendMoneyAsync((int) mMoney2);
            await User1.AwardMoneyAsync((int) mMoney2);

            await User1.SpendConquerPointsAsync((int) mEmoney1);
            await User2.AwardConquerPointsAsync((int) mEmoney1);

            await User2.SpendConquerPointsAsync((int) mEmoney2);
            await User1.AwardConquerPointsAsync((int) mEmoney2);

            var dbItemsRecordTrack = new List<DbTradeItem>(41);
            foreach (Item item in mDicItems1.Values)
            {
                if (item.IsMonopoly() || item.IsBound)
                    continue;

                await User1.UserPackage.RemoveFromInventoryAsync(item, UserPackage.RemovalType.RemoveAndDisappear);
                await item.ChangeOwnerAsync(User2.Identity, Item.ChangeOwnerType.TradeItem);
                await User2.UserPackage.AddItemAsync(item);

                dbItemsRecordTrack.Add(new DbTradeItem
                {
                    TradeIdentity = dbTrade.Identity,
                    SenderIdentity = User1.Identity,
                    ItemIdentity = item.Identity,
                    Itemtype = item.Type,
                    Chksum = (uint) item.ToJson().GetHashCode(),
                    JsonData = item.ToJson()
                });
            }

            foreach (Item item in mDicItems2.Values)
            {
                if (item.IsMonopoly() || item.IsBound)
                    continue;

                await User2.UserPackage.RemoveFromInventoryAsync(item, UserPackage.RemovalType.RemoveAndDisappear);
                await item.ChangeOwnerAsync(User1.Identity, Item.ChangeOwnerType.TradeItem);
                await User1.UserPackage.AddItemAsync(item);

                dbItemsRecordTrack.Add(new DbTradeItem
                {
                    TradeIdentity = dbTrade.Identity,
                    SenderIdentity = User2.Identity,
                    ItemIdentity = item.Identity,
                    Itemtype = item.Type,
                    Chksum = (uint) item.ToJson().GetHashCode(),
                    JsonData = item.ToJson()
                });
            }

            await ServerDbContext.SaveAsync(dbItemsRecordTrack);

            await User1.SendAsync(Language.StrTradeSuccess);
            await User2.SendAsync(Language.StrTradeSuccess);
        }

        public async Task SendCloseAsync()
        {
            User1.Trade = null;
            User2.Trade = null;

            if (User1.IsConnected)
                await User1.SendAsync(new MsgTrade
                {
                    Action = MsgTrade<Client>.TradeAction.Fail,
                    Data = User2.Identity
                });

            if (User2.IsConnected)
                await User2.SendAsync(new MsgTrade
                {
                    Action = MsgTrade<Client>.TradeAction.Fail,
                    Data = User1.Identity
                });
        }

        private MsgTrade RemoveMsg(uint id)
        {
            return new MsgTrade
            {
                Action = MsgTrade<Client>.TradeAction.AddItemFail,
                Data = id
            };
        }
    }
}