using System.Collections.Generic;
using System.Threading.Tasks;
using Comet.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Comet.Game.Database.Repositories
{
    public static class TrapRepository
    {
        public static async Task<List<DbTrap>> GetAsync()
        {
            await using var ctx = new ServerDbContext();
            return await ctx.Traps
                            .Include(x => x.Type)
                            .ToListAsync();
        }
    }
}