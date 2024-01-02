using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Comet.Database.Entities;

namespace Comet.Game.Database.Repositories
{
    public static class SyndicateAllyRepository
    {
        public static async Task<List<DbSyndicateAlly>> GetAsync(uint id)
        {
            await using var db = new ServerDbContext();
            return db.SyndicatesAlly.Where(x => x.SyndicateIdentity == id).ToList();
        }

        public static async Task DeleteAsync(uint id0, uint id1)
        {
            DbSyndicateAlly ally = (await GetAsync(id0)).FirstOrDefault(x => x.AllyIdentity == id1);
            if (ally != null)
                await ServerDbContext.DeleteAsync(ally);

            ally = (await GetAsync(id1)).FirstOrDefault(x => x.AllyIdentity == id0);
            if (ally != null)
                await ServerDbContext.DeleteAsync(ally);
        }
    }
}