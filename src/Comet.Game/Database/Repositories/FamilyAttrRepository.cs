using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Comet.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Comet.Game.Database.Repositories
{
    public static class FamilyAttrRepository
    {
        public static async Task<List<DbFamilyAttr>> GetAsync(uint idFamily)
        {
            await using var ctx = new ServerDbContext();
            return await ctx.FamilyAttrs.Where(x => x.FamilyIdentity == idFamily).ToListAsync();
        }
    }
}