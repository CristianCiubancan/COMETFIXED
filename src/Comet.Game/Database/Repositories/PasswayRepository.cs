using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Comet.Database.Entities;

namespace Comet.Game.Database.Repositories
{
    public static class PasswayRepository
    {
        public static async Task<List<DbPassway>> GetAsync(uint idMap)
        {
            await using var context = new ServerDbContext();
            return context.Passway.Where(x => x.MapId == idMap).ToList();
        }
    }
}