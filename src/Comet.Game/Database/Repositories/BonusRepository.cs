using System.Threading.Tasks;
using Comet.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Comet.Game.Database.Repositories
{
    public static class BonusRepository
    {
        public static async Task<DbBonus> GetAsync(uint account)
        {
            await using var db = new ServerDbContext();
            return await db.Bonus.FirstOrDefaultAsync(
                       x => x.AccountIdentity == account && x.Flag == 0 && x.Time == null);
        }

        public static async Task<int> CountAsync(uint account)
        {
            await using var db = new ServerDbContext();
            return await db.Bonus.CountAsync(x => x.AccountIdentity == account && x.Flag == 0 && x.Time == null);
        }
    }
}