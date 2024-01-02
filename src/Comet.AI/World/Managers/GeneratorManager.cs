using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Comet.AI.Database.Repositories;
using Comet.AI.World.Maps;
using Comet.Database.Entities;
using Comet.Shared;

namespace Comet.AI.World.Managers
{
    public static class GeneratorManager
    {
        public static async Task<bool> InitializeAsync()
        {
            try
            {
                foreach (DbGenerator dbGen in await GeneratorRepository.GetAsync())
                {
                    var gen = new Generator(dbGen);
                    if (gen.CanBeProcessed) await AddGeneratorAsync(gen);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public static async Task OnTimerAsync()
        {
            var sw = new Stopwatch();
            sw.Start();
            foreach (int partition in mGenerators.Keys)
            foreach (Generator gen in mGenerators[partition])
                await gen.GenerateAsync();
            sw.Stop();
        }

        public static async Task<bool> AddGeneratorAsync(Generator generator)
        {
            try
            {
                if (!generator.CanBeProcessed)
                    return false;

                GameMap map = MapManager.GetMap(generator.MapIdentity);
                if (mGenerators.ContainsKey(map.Partition))
                {
                    mGenerators[map.Partition].Add(generator);
                }
                else
                {
                    mGenerators.TryAdd(map.Partition, new List<Generator>());
                    mGenerators[map.Partition].Add(generator);
                }
            }
            catch (Exception e)
            {
                await Log.WriteLogAsync(LogLevel.Exception, e.ToString());
                return false;
            }

            return true;
        }

        public static async Task SynchroGeneratorsAsync()
        {
            if (Kernel.GameServer == null || !Kernel.GameServer.Socket.Connected)
                return;

            var count = 0;
            foreach (List<Generator> partition in mGenerators.Values)
            foreach (Generator generator in partition)
                count += await generator.SendAllAsync();

            await Log.WriteLogAsync($"Total {count} NPCs sent to the game server!!!");
        }

        public static Generator GetGenerator(uint idGen)
        {
            return mGenerators.Keys.SelectMany(partition => mGenerators[partition])
                              .FirstOrDefault(gen => gen.Identity == idGen);
        }

        public static List<Generator> GetGenerators(uint idMap, string monsterName)
        {
            return (from partition in mGenerators.Keys
                    from gen in mGenerators[partition]
                    where gen.MapIdentity == idMap && gen.MonsterName.Equals(monsterName)
                    select gen).ToList();
        }

        public static List<Generator> GetByMonsterType(uint idType)
        {
            return (from partition in mGenerators.Keys
                    from gen in mGenerators[partition]
                    where gen.RoleType == idType
                    select gen).ToList();
        }

        private static readonly ConcurrentDictionary<int, List<Generator>> mGenerators = new();
    }
}