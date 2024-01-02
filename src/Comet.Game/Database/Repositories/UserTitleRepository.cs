using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Comet.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Comet.Game.Database.Repositories
{
    public static class UserTitleRepository
    {
        public static async Task<List<DbUserTitle>> GetAsync(uint idPlayer)
        {
            await using var ctx = new ServerDbContext();
            return await ctx.UserTitle
                            .Where(x => x.PlayerId == idPlayer && x.DelTime > DateTime.Now)
                            .ToListAsync();
        }
    }
}