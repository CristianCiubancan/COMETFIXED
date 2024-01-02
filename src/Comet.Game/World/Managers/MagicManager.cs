using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Comet.Database.Entities;
using Comet.Game.Database.Repositories;

namespace Comet.Game.World.Managers
{
    public static class MagicManager
    {
        private static readonly ConcurrentDictionary<uint, DbMagictype> m_magicType = new();

        public static async Task InitializeAsync()
        {
            foreach (DbMagictype magicType in await MagictypeRepository.GetAsync())
                m_magicType.TryAdd(magicType.Id, magicType);
        }

        public static byte GetMaxLevel(uint idType)
        {
            return (byte) (m_magicType.Values.Where(x => x.Type == idType).OrderByDescending(x => x.Level)
                                      .FirstOrDefault()?.Level ?? 0);
        }

        public static DbMagictype GetMagictype(uint idType, ushort level)
        {
            return m_magicType.Values.FirstOrDefault(x => x.Type == idType && x.Level == level);
        }
    }
}