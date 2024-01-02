using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Comet.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Comet.Game.Database.Repositories
{
    public sealed class CardRepository
    {
        public static async Task<List<DbCard>> GetAsync(uint accountId)
        {
            await using var ctx = new ServerDbContext();
            return await ctx.Cards.Where(x => x.AccountId == accountId && x.Flag == 0 && x.Timestamp == null)
                            .ToListAsync();
        }

        public static async Task<int> CountAsync(uint account)
        {
            await using var db = new ServerDbContext();
            return await db.Cards.CountAsync(x => x.AccountId == account && x.Flag == 0 && x.Timestamp == null);
        }
    }
}