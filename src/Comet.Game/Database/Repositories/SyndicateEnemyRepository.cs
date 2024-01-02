using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Comet.Database.Entities;

namespace Comet.Game.Database.Repositories
{
    public static class SyndicateEnemyRepository
    {
        public static async Task<List<DbSyndicateEnemy>> GetAsync(uint id)
        {
            await using var db = new ServerDbContext();
            return db.SyndicatesEnemy.Where(x => x.SyndicateIdentity == id).ToList();
        }

        public static async Task<bool> DeleteAsync(uint id0, uint id1)
        {
            DbSyndicateEnemy enemy = (await GetAsync(id0)).FirstOrDefault(x => x.EnemyIdentity == id1);
            return enemy != null && await ServerDbContext.DeleteAsync(enemy);
        }
    }
}