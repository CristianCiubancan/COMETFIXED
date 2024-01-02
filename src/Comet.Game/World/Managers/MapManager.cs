using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Comet.Core.World;
using Comet.Database.Entities;
using Comet.Game.Database.Repositories;
using Comet.Game.States;
using Comet.Game.World.Maps;
using Comet.Shared;

namespace Comet.Game.World.Managers
{
    public sealed class MapManager
    {
        public static ConcurrentDictionary<uint, GameMap> GameMaps { get; } = new();

        public static async Task LoadMapsAsync()
        {
            await MapDataManager.LoadDataAsync().ConfigureAwait(true);

            List<DbMap> maps = await MapsRepository.GetAsync();
            foreach (DbMap dbmap in maps)
            {
                var map = new GameMap(dbmap);
                if (await map.InitializeAsync())
                {
#if DEBUG
                    await Log.WriteLogAsync(LogLevel.Debug,$"Map[{map.Identity:000000}] MapDoc[{map.MapDoc:0000}] {map.Name,-32} Partition: {map.Partition:00} loaded...");
#endif
                    GameMaps.TryAdd(map.Identity, map);
                }
                else
                {
                    await Log.WriteLogAsync(LogLevel.Error, "Could not load ");
                }
            }

            List<DbDynamap> dynaMaps = await MapsRepository.GetDynaAsync();
            foreach (DbDynamap dbmap in dynaMaps)
            {
                var map = new GameMap(dbmap);
                if (await map.InitializeAsync())
                {
#if DEBUG
                    await Log.WriteLogAsync(LogLevel.Debug,$"Map[{map.Identity:0000000}] MapDoc[{map.MapDoc:0000000}] {map.Name,-32} Partition: {map.Partition:00} loaded...");
#endif
                    GameMaps.TryAdd(map.Identity, map);
                }
            }

#if DEBUG
            const string partitionLogFile = "MapPartition";
            string path = Path.Combine(Environment.CurrentDirectory, $"{partitionLogFile}.log");
            if (File.Exists(path))
                File.Delete(path);
#endif
            foreach (GameMap map in GameMaps.Values.OrderBy(x => x.Partition).ThenBy(x => x.Identity))
            {
#if DEBUG
                await Log.WriteToFile(partitionLogFile, LogFolder.Root, $"Map[{map.Identity:0000000}] {map.Name,-32} Partition: {map.Partition:00}");
#endif
                await map.LoadTrapsAsync();
            }
        }

        public static GameMap GetMap(uint idMap)
        {
            return GameMaps.TryGetValue(idMap, out GameMap value) ? value : null;
        }

        public static async Task<bool> AddMapAsync(GameMap map)
        {
            if (GameMaps.TryAdd(map.Identity, map))
            {
                await map.SendAddToNpcServerAsync();
                return true;
            }

            return false;
        }

        public static async Task<bool> RemoveMapAsync(uint idMap)
        {
            GameMaps.TryRemove(idMap, out GameMap map);
            await map.SendRemoveToNpcServerAsync();
            return true;
        }

        public static async Task OnTimerAsync()
        {
            foreach (GameMap map in GameMaps.Values.Where(x => x.PlayerCount > 0))
            foreach (Role mob in map.QueryRoles(x => !x.IsPlayer()))
                await mob.OnTimerAsync();
        }
    }
}