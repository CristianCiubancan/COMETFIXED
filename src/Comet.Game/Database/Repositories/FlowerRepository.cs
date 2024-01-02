using System.Collections.Generic;
using System.Threading.Tasks;
using Comet.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Comet.Game.Database.Repositories
{
    public static class FlowerRepository
    {
        public static async Task<List<DbFlower>> GetAsync()
        {
            await using var ctx = new ServerDbContext();
            return await ctx.Flowers
                            .Include(x => x.User)
                            .ToListAsync();
        }
    }
}