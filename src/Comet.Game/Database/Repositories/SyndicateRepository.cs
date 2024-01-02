using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Comet.Database.Entities;

namespace Comet.Game.Database.Repositories
{
    public static class SyndicateRepository
    {
        public static async Task<List<DbSyndicate>> GetAsync()
        {
            await using var db = new ServerDbContext();
            return db.Syndicates.ToList();
        }
    }
}