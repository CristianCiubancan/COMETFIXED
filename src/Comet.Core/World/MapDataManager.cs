using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using Comet.Core.World.Maps;
using Comet.Shared;

namespace Comet.Core.World
{
    public static class MapDataManager
    {
        public static async Task LoadDataAsync()
        {
            FileStream stream = File.OpenRead(string.Format(".{0}ini{0}GameMap.dat", Path.DirectorySeparatorChar));
            var reader = new BinaryReader(stream);

            int mapDataCount = reader.ReadInt32();
            await Log.WriteLogAsync(LogLevel.Debug, $"Loading {mapDataCount} maps...");

            for (var i = 0; i < mapDataCount; i++)
            {
                uint idMap = reader.ReadUInt32();
                int length = reader.ReadInt32();
                var name = new string(reader.ReadChars(length));
                uint puzzle = reader.ReadUInt32();

                m_mapData.TryAdd(idMap, new MapData
                {
                    ID = idMap,
                    Length = length,
                    Name = name,
                    Puzzle = puzzle
                });
            }

            reader.Close();
            stream.Close();
            reader.Dispose();
            await stream.DisposeAsync();
        }

        public static GameMapData GetMapData(uint idDoc)
        {
            if (!m_mapData.TryGetValue(idDoc, out MapData value))
                return null;

            GameMapData mapData = new(idDoc);
            if (mapData.Load(value.Name.Replace("\\", Path.DirectorySeparatorChar.ToString()))) return mapData;
            return null;
        }

        private struct MapData
        {
            public uint ID;
            public int Length;
            public string Name;
            public uint Puzzle;
        }

        private static readonly ConcurrentDictionary<uint, MapData> m_mapData = new();
    }
}