using System.Collections.Generic;
using System.Threading.Tasks;
using Comet.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Comet.Game.Database.Repositories
{
    public static class TutorTypeRepository
    {
        public static async Task<List<DbTutorType>> GetAsync()
        {
            await using var ctx = new ServerDbContext();
            return await ctx.TutorTypes.ToListAsync();
        }
    }
}