using System.Collections.Generic;
using System.Threading.Tasks;
using Comet.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Comet.Game.Database.Repositories
{
    internal class SupermanRepository
    {
        public static async Task<List<DbSuperman>> GetAsync()
        {
            await using var ctx = new ServerDbContext();
            return await ctx.Superman.ToListAsync();
        }
    }
}