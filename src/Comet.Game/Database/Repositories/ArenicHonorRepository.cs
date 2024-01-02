using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Comet.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Comet.Game.Database.Repositories
{
    public static class ArenicHonorRepository
    {
        public static async Task<List<DbArenicHonor>> GetAsync(byte type)
        {
            await using var ctx = new ServerDbContext();
            return await ctx.ArenicHonors.Where(x => x.Type == type).ToListAsync();
        }
    }
}