using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Comet.AI.Database.Repositories;
using Comet.AI.States;
using Comet.AI.World.Maps;
using Comet.Core.World;
using Comet.Database.Entities;
using Comet.Shared;

namespace Comet.AI.World.Managers
{
    public static class MapManager
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
                    await Log.WriteLogAsync(LogLevel.Debug,
                                            $"Map[{map.Identity:000000}] MapDoc[{map.MapDoc:0000}] {map.Name:-32} Partition: {map.Partition:00} loaded...");
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
                    GameMaps.TryAdd(map.Identity, map);
                    await Log.GmLogAsync("map_channel", $"{map.Identity}\t{map.Name}\t\t\tPartition: {map.Partition}");
                }
            }
        }

        public static GameMap GetMap(uint idMap)
        {
            return GameMaps.TryGetValue(idMap, out GameMap value) ? value : null;
        }

        public static bool AddMap(GameMap map)
        {
            return GameMaps.TryAdd(map.Identity, map);
        }

        public static bool RemoveMap(uint idMap)
        {
            return GameMaps.TryRemove(idMap, out _);
        }

        public static async Task OnTimerAsync()
        {
            foreach (GameMap map in GameMaps.Values.Where(x => x.PlayerCount > 0))
            foreach (Role mob in map.QueryRoles(x => x.IsMonster()))
                await mob.OnTimerAsync();
        }
    }
}