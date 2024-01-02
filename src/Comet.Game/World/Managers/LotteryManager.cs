using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Comet.Core;
using Comet.Database.Entities;
using Comet.Game.Database.Repositories;
using Comet.Game.States;
using Comet.Game.States.Items;
using Comet.Network.Packets.Game;
using Comet.Shared;

namespace Comet.Game.World.Managers
{
    public static class LotteryManager
    {
        private static readonly List<DbConfig> _config = new();

        public static async Task<bool> InitializeAsync()
        {
            for (var i = 0; i < 5; i++)
            {
                int minConfig = 11000 + i * 10;
                int maxConfig = 11009 + i * 10;

                _config.AddRange(await ConfigRepository.GetAsync(x => x.Type >= minConfig && x.Type <= maxConfig));
            }

            return true;
        }

        public static async Task<bool> QueryPrizeAsync(Character user, int pool)
        {
            List<DbLottery> allItems = await LotteryRepository.GetAsync();
            List<DbConfig> lotteryConfiguration = _config.Where(x => x.Data1 == pool).ToList();

            var ranks = new List<LotteryRankTempInfo>();
            var chance = 0;
            foreach (DbConfig config in lotteryConfiguration.OrderBy(x => x.Data2))
            {
                chance += config.Data2;
                ranks.Add(new LotteryRankTempInfo
                {
                    Chance = chance,
                    Rank = config.Type % 10
                });
            }

            LotteryRankTempInfo tempRank = default;
            int rand = await Kernel.NextAsync(chance);
            foreach (LotteryRankTempInfo rank in ranks)
            {
                if (rand <= rank.Chance)
                {
                    tempRank = rank;
                    break;
                }
            }

            chance = 0;
            var infos = new List<LotteryItemTempInfo>();
            foreach (DbLottery item in allItems.Where(x => x.Rank == tempRank.Rank && x.Color == pool))
            {
                chance += (int) item.Chance;
                infos.Add(new LotteryItemTempInfo
                {
                    Chance = chance,
                    ItemIdentity = item.ItemIdentity,
                    ItemName = item.Itemname,
                    Plus = item.Plus,
                    Color = item.Color,
                    SocketNum = item.SocketNum
                });
            }

            LotteryItemTempInfo reward = default;
            rand = await Kernel.NextAsync(chance);
            foreach (LotteryItemTempInfo info in infos.OrderBy(x => x.Chance))
            {
                if (rand <= info.Chance)
                {
                    reward = info;
                    break;
                }
            }

            DbItemtype itemType = ItemManager.GetItemtype(reward.ItemIdentity);
            if (itemType == null)
                return false;

            var lottoItem = new DbItem
            {
                Type = reward.ItemIdentity,
                Amount = itemType.Amount,
                AmountLimit = itemType.AmountLimit,
                Magic3 = reward.Plus > 0 ? reward.Plus : itemType.Magic3,
                Gem1 = (byte) (reward.SocketNum > 0 ? 255 : 0),
                Gem2 = (byte) (reward.SocketNum > 1 ? 255 : 0),
                Color = 3,
                PlayerId = user.Identity
            };

            var newItem = new Item(user);
            if (!await newItem.CreateAsync(lottoItem))
            {
                await Log.WriteLogAsync(LogLevel.Error, $"Error to create reward item {newItem.ToJson()}");
                return false;
            }

            await user.UserPackage.AddItemAsync(newItem);

            await Log.GmLogAsync(
                "lottery",
                $"{user.Identity},{user.Name},{tempRank.Rank},{reward.Color},{newItem.Type},{newItem.Plus},{newItem.SocketOne},{newItem.SocketTwo}");

            if (tempRank.Rank <= 5)
                await RoleManager.BroadcastMsgAsync(string.Format(Language.StrLotteryHigh, user.Name, reward.ItemName),
                                                    TalkChannel.Talk);
            else
                await user.SendAsync(string.Format(Language.StrLotteryLow, reward.ItemName));

            return true;
        }

        private struct LotteryRankTempInfo
        {
            public int Chance { get; init; }
            public int Rank { get; init; }
        }

        private struct LotteryItemTempInfo
        {
            public int Chance { get; init; }
            public string ItemName { get; init; }
            public uint ItemIdentity { get; init; }
            public byte Color { get; init; }
            public byte SocketNum { get; init; }
            public byte Plus { get; init; }
        }
    }
}