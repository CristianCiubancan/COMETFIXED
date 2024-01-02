using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Comet.Database.Entities;
using Comet.Game.Database;
using Comet.Game.Database.Repositories;
using Comet.Game.States;
using Comet.Network.Packets.Game;

namespace Comet.Game.World.Managers
{
    public static class FlowerManager
    {
        private static readonly ConcurrentDictionary<uint, FlowerRankObject> m_dicFlowers = new();

        public static async Task<bool> InitializeAsync()
        {
            foreach (DbFlower flower in await FlowerRepository.GetAsync())
            {
                var obj = new FlowerRankObject(flower);
                m_dicFlowers.TryAdd(flower.UserId, obj);
            }

            return true;
        }

        public static List<FlowerRankingStruct> GetFlowerRanking(MsgFlower<Client>.FlowerType type, int from = 0,
                                                                 int limit = 10)
        {
            var position = 1;
            switch (type)
            {
                case MsgFlower<Client>.FlowerType.RedRose:
                    return m_dicFlowers.Values.Where(x => (x.Mesh % 10000 - x.Mesh % 10) / 1000 == 2 && x.RedRose > 0)
                                       .OrderByDescending(x => x.RedRose).Skip(from)
                                       .Take(limit).Select(x => new FlowerRankingStruct
                                       {
                                           Identity = x.UserIdentity,
                                           Name = x.Name,
                                           Profession = (ushort) x.Profession,
                                           Value = x.RedRose,
                                           Position = position++
                                       }).ToList();

                case MsgFlower<Client>.FlowerType.WhiteRose:
                    return m_dicFlowers.Values.Where(x => (x.Mesh % 10000 - x.Mesh % 10) / 1000 == 2 && x.WhiteRose > 0)
                                       .OrderByDescending(x => x.WhiteRose)
                                       .Skip(from).Take(limit).Select(x => new FlowerRankingStruct
                                       {
                                           Identity = x.UserIdentity,
                                           Name = x.Name,
                                           Profession = (ushort) x.Profession,
                                           Value = x.WhiteRose,
                                           Position = position++
                                       }).ToList();

                case MsgFlower<Client>.FlowerType.Orchid:
                    return m_dicFlowers.Values.Where(x => (x.Mesh % 10000 - x.Mesh % 10) / 1000 == 2 && x.Orchids > 0)
                                       .OrderByDescending(x => x.Orchids).Skip(from)
                                       .Take(limit).Select(x => new FlowerRankingStruct
                                       {
                                           Identity = x.UserIdentity,
                                           Name = x.Name,
                                           Profession = (ushort) x.Profession,
                                           Value = x.Orchids,
                                           Position = position++
                                       }).ToList();

                case MsgFlower<Client>.FlowerType.Tulip:
                    return m_dicFlowers.Values.Where(x => (x.Mesh % 10000 - x.Mesh % 10) / 1000 == 2 && x.Tulips > 0)
                                       .OrderByDescending(x => x.Tulips).Skip(from)
                                       .Take(limit).Select(x => new FlowerRankingStruct
                                       {
                                           Identity = x.UserIdentity,
                                           Name = x.Name,
                                           Profession = (ushort) x.Profession,
                                           Value = x.Tulips,
                                           Position = position++
                                       }).ToList();
            }

            return new List<FlowerRankingStruct>();
        }

        public static List<FlowerRankingStruct> GetFlowerRankingToday(MsgFlower<Client>.FlowerType type, int from = 0,
                                                                      int limit = 10)
        {
            var position = 1;
            switch (type)
            {
                case MsgFlower<Client>.FlowerType.RedRose:
                    return m_dicFlowers
                           .Values.Where(x => (x.Mesh % 10000 - x.Mesh % 10) / 1000 == 2 && x.RedRoseToday > 0)
                           .OrderByDescending(x => x.RedRoseToday).Skip(from)
                           .Take(limit).Select(x => new FlowerRankingStruct
                           {
                               Identity = x.UserIdentity,
                               Name = x.Name,
                               Profession = (ushort) x.Profession,
                               Value = x.RedRoseToday,
                               Position = position++
                           }).ToList();

                case MsgFlower<Client>.FlowerType.WhiteRose:
                    return m_dicFlowers
                           .Values.Where(x => (x.Mesh % 10000 - x.Mesh % 10) / 1000 == 2 && x.WhiteRoseToday > 0)
                           .OrderByDescending(x => x.WhiteRoseToday)
                           .Skip(from).Take(limit).Select(x => new FlowerRankingStruct
                           {
                               Identity = x.UserIdentity,
                               Name = x.Name,
                               Profession = (ushort) x.Profession,
                               Value = x.WhiteRoseToday,
                               Position = position++
                           }).ToList();

                case MsgFlower<Client>.FlowerType.Orchid:
                    return m_dicFlowers
                           .Values.Where(x => (x.Mesh % 10000 - x.Mesh % 10) / 1000 == 2 && x.OrchidsToday > 0)
                           .OrderByDescending(x => x.OrchidsToday).Skip(from)
                           .Take(limit).Select(x => new FlowerRankingStruct
                           {
                               Identity = x.UserIdentity,
                               Name = x.Name,
                               Profession = (ushort) x.Profession,
                               Value = x.OrchidsToday,
                               Position = position++
                           }).ToList();

                case MsgFlower<Client>.FlowerType.Tulip:
                    return m_dicFlowers
                           .Values.Where(x => (x.Mesh % 10000 - x.Mesh % 10) / 1000 == 2 && x.TulipsToday > 0)
                           .OrderByDescending(x => x.TulipsToday).Skip(from)
                           .Take(limit).Select(x => new FlowerRankingStruct
                           {
                               Identity = x.UserIdentity,
                               Name = x.Name,
                               Profession = (ushort) x.Profession,
                               Value = x.TulipsToday,
                               Position = position++
                           }).ToList();
            }

            return new List<FlowerRankingStruct>();
        }

        public static async Task<FlowerRankObject> QueryFlowersAsync(Character user)
        {
            if (m_dicFlowers.TryGetValue(user.Identity, out FlowerRankObject value))
                return value;
            if (m_dicFlowers.TryAdd(user.Identity, value = new FlowerRankObject(user)))
            {
                await ServerDbContext.SaveAsync(value.GetDatabaseObject());
                return value;
            }

            return null;
        }

        public static async Task DailyResetAsync()
        {
            await ServerDbContext.DeleteAsync(m_dicFlowers.Values.Select(x => x.GetDatabaseObject()).ToList());
            m_dicFlowers.Clear();
        }

        public class FlowerRankObject
        {
            private readonly DbFlower m_flower;

            public FlowerRankObject(DbFlower flower)
            {
                m_flower = flower;

                if (flower.User == null)
                    return;

                Mesh = flower.User.Mesh;
                Name = flower.User.Name;
                Level = flower.User.Level;
                Profession = flower.User.Profession;
                Metempsychosis = flower.User.Rebirths;

                RedRose = flower.User.FlowerRed;
                WhiteRose = flower.User.FlowerWhite;
                Orchids = flower.User.FlowerOrchid;
                Tulips = flower.User.FlowerTulip;
            }

            public FlowerRankObject(Character user)
            {
                m_flower = new DbFlower
                {
                    UserId = user.Identity
                };

                Mesh = user.Mesh;
                Name = user.Name;
                Level = user.Level;
                Metempsychosis = user.Metempsychosis;
                Profession = user.Profession;

                RedRose = user.FlowerRed;
                WhiteRose = user.FlowerWhite;
                Orchids = user.FlowerOrchid;
                Tulips = user.FlowerTulip;
            }

            public uint UserIdentity => m_flower.UserId;
            public uint Mesh { get; }
            public string Name { get; }
            public int Level { get; }
            public int Profession { get; }
            public int Metempsychosis { get; }

            public uint RedRose { get; set; }
            public uint WhiteRose { get; set; }
            public uint Orchids { get; set; }
            public uint Tulips { get; set; }

            public uint RedRoseToday
            {
                get => m_flower.RedRose;
                set => m_flower.RedRose = value;
            }

            public uint WhiteRoseToday
            {
                get => m_flower.WhiteRose;
                set => m_flower.WhiteRose = value;
            }

            public uint OrchidsToday
            {
                get => m_flower.Orchids;
                set => m_flower.Orchids = value;
            }

            public uint TulipsToday
            {
                get => m_flower.Tulips;
                set => m_flower.Tulips = value;
            }

            public Task SaveAsync()
            {
                return ServerDbContext.SaveAsync(m_flower);
            }

            public DbFlower GetDatabaseObject()
            {
                return m_flower;
            }
        }

        public struct FlowerRankingStruct
        {
            public int Position;
            public uint Identity;
            public string Name;
            public ushort Profession;
            public uint Value;
        }
    }
}