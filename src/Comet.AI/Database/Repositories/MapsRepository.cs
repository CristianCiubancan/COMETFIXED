using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Comet.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Comet.AI.Database.Repositories
{
    public static class MapsRepository
    {
        public static async Task<DbMap> GetAsync(uint idMap)
        {
            await using var db = new ServerDbContext();
            return await db.Maps.FirstOrDefaultAsync(x => x.Identity == idMap);
        }

        public static async Task<List<DbMap>> GetAsync()
        {
            await using var db = new ServerDbContext();
            return db.Maps.Where(x => x.ServerIndex == -1 ||
                                      x.ServerIndex == Kernel.Configuration.GameNetwork.ServerIdentity)
                     .ToList();
        }

        public static async Task<List<DbDynamap>> GetDynaAsync()
        {
            await using var db = new ServerDbContext();
            return db.DynaMaps.Where(x => x.ServerIndex == -1 ||
                                          x.ServerIndex == Kernel.Configuration.GameNetwork.ServerIdentity)
                     .ToList();
        }
    }
}