using System.Collections.Generic;
using System.Threading.Tasks;
using Comet.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Comet.Game.Database.Repositories
{
    public static class PigeonQueueRepository
    {
        public static async Task<List<DbPigeonQueue>> GetAsync()
        {
            await using var ctx = new ServerDbContext();
            return await ctx.PigeonQueues.ToListAsync();
        }
    }
}